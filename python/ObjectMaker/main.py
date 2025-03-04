import os
import base64
import zipfile
from fastapi import FastAPI, File, UploadFile, HTTPException
from fastapi.responses import JSONResponse
import subprocess

app = FastAPI()

UPLOAD_FOLDER = 'uploads'
MESHROOM_BIN_PATH = 'E:\\Meshroom-2023.3.0\\aliceVision'
MESHROOM_EXE = 'E:\\Meshroom-2023.3.0\\meshroom_batch.exe'
DRAFT_PIPELINE = 'E:\\Meshroom-2023.3.0\\lib\\meshroom\\pipelines\\photogrammetryDraft.mg'

os.makedirs(UPLOAD_FOLDER, exist_ok=True)

@app.post("/upload_and_process", summary="Upload a ZIP file containing images and generate a 3D model using Meshroom Draft Meshing")
async def upload_and_process(file: UploadFile = File(...)):
    """
    Upload a ZIP file containing images for 3D reconstruction and process them using Meshroom with Draft Meshing.
    """
    try:
        # Save the ZIP file temporarily
        zip_path = os.path.join(UPLOAD_FOLDER, file.filename)
        with open(zip_path, "wb") as f:
            f.write(await file.read())

        print(f"ZIP file saved at: {zip_path}")  # Debug print

        # Extract the ZIP file
        with zipfile.ZipFile(zip_path, 'r') as zip_ref:
            zip_ref.extractall(UPLOAD_FOLDER)

        print(f"Extracted files to: {UPLOAD_FOLDER}")  # Debug print

        # Move images from any nested folder to the root uploads folder
        for root, dirs, files in os.walk(UPLOAD_FOLDER):
            for dir_name in dirs:
                dir_path = os.path.join(root, dir_name)
                for item in os.listdir(dir_path):
                    source = os.path.join(dir_path, item)
                    if os.path.isfile(source):
                        destination = os.path.join(UPLOAD_FOLDER, item)
                        os.rename(source, destination)

                # Remove the empty folder after moving images
                os.rmdir(dir_path)

        # Confirm that images are now in the uploads folder
        if not any(file.endswith(('.jpg', '.png', '.jpeg')) for file in os.listdir(UPLOAD_FOLDER)):
            raise HTTPException(status_code=500, detail="No valid images found after extraction.")

        # Process the images to generate a 3D model using Meshroom (Draft Meshing for faster CPU processing)
        output_path = os.path.join(UPLOAD_FOLDER, 'output')
        os.makedirs(output_path, exist_ok=True)

        # Define Meshroom pipeline command with Draft Meshing
        meshroom_command = [
            MESHROOM_EXE,
            '--input', UPLOAD_FOLDER,  # Input images directory
            '--output', output_path,   # Output directory
            '-p', DRAFT_PIPELINE       # Use Draft Meshing pipeline
        ]

        print(f"Running Meshroom command: {' '.join(meshroom_command)}")  # Debug print
        subprocess.run(meshroom_command, check=True)

        # Check if the 3D model was generated
        obj_output_path = os.path.join(output_path, 'texturedMesh.obj')
        mtl_output_path = os.path.join(output_path, 'texturedMesh.mtl')
        exr_output_path = os.path.join(output_path, 'texturedMesh.exr')

        if not os.path.exists(obj_output_path):
            raise HTTPException(status_code=500, detail="3D model generation failed.")

        print(f"3D model generated at: {obj_output_path}")  # Debug print

        print(f"3D model generated at: {obj_output_path}")  # Debug print

        # Create a ZIP file with the generated .obj, .mtl, and .exr files
        zip_filename = "3D_model_output.zip"
        zip_filepath = os.path.join(output_path, zip_filename)

        with zipfile.ZipFile(zip_filepath, 'w', zipfile.ZIP_DEFLATED) as zipf:
            zipf.write(obj_output_path, os.path.basename(obj_output_path))
            if os.path.exists(mtl_output_path):
                zipf.write(mtl_output_path, os.path.basename(mtl_output_path))
            if os.path.exists(exr_output_path):
                zipf.write(exr_output_path, os.path.basename(exr_output_path))

        print(f"Created ZIP file at: {zip_filepath}")  # Debug print

        # Convert ZIP file to Base64
        with open(zip_filepath, "rb") as zip_file:
            zip_base64 = base64.b64encode(zip_file.read()).decode("utf-8")

        # Delete the ZIP file after conversion
        os.remove(zip_filepath)
        print("Deleted ZIP file after conversion.")  # Debug print

        # Clean up images in the upload folder
        for filename in os.listdir(UPLOAD_FOLDER):
            file_path = os.path.join(UPLOAD_FOLDER, filename)
            if os.path.isfile(file_path):
                os.remove(file_path)

        print("Cleaned up images in upload folder.")  # Debug print

        # Return success response with the output file path
        return JSONResponse(content={'message': '3D reconstruction completed', 'zip_base64': zip_base64})

    except Exception as e:
        print(f"Error: {str(e)}")  # Print error details to the console
        raise HTTPException(status_code=500, detail=f"Error during upload or processing: {str(e)}")


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=8000)
