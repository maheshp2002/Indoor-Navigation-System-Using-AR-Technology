from fastapi import FastAPI, File, UploadFile
from fastapi.responses import JSONResponse
import os
import trimesh
from pathlib import Path

app = FastAPI()

# Directory to save uploaded files
UPLOAD_DIR = "uploads"
Path(UPLOAD_DIR).mkdir(exist_ok=True)


# Endpoint to handle GLB file upload and conversion
@app.post("/upload/glb")
async def upload_glb(file: UploadFile = File(...)):
    # Ensure the file is .glb
    if not file.filename.endswith(".glb"):
        return JSONResponse(content={"error": "Invalid file format. Please upload a .glb file."}, status_code=400)

    # Save the uploaded GLB file
    glb_path = os.path.join(UPLOAD_DIR, file.filename)
    with open(glb_path, "wb") as buffer:
        buffer.write(await file.read())

    # Load the GLB file with trimesh
    try:
        mesh = trimesh.load(glb_path, file_type="glb")
    except Exception as e:
        return JSONResponse(content={"error": f"Error loading GLB file: {str(e)}"}, status_code=500)

    # Convert GLB to OBJ
    obj_path = os.path.splitext(glb_path)[0] + ".obj"
    try:
        mesh.export(obj_path, file_type="obj")
    except Exception as e:
        return JSONResponse(content={"error": f"Error converting GLB to OBJ: {str(e)}"}, status_code=500)

    return JSONResponse(content={"message": "GLB file converted to OBJ successfully", "obj_path": obj_path})


# Endpoint to handle GeoTIFF file upload
@app.post("/upload/geotiff")
async def upload_geotiff(file: UploadFile = File(...)):
    # Ensure the file is .tiff or .tif
    if not (file.filename.endswith(".tiff") or file.filename.endswith(".tif")):
        return JSONResponse(content={"error": "Invalid file format. Please upload a GeoTIFF file."}, status_code=400)

    # Save the uploaded GeoTIFF file
    geotiff_path = os.path.join(UPLOAD_DIR, file.filename)
    with open(geotiff_path, "wb") as buffer:
        buffer.write(await file.read())

    return JSONResponse(content={"message": "GeoTIFF file uploaded successfully", "geotiff_path": geotiff_path})


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=8000)
