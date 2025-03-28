Translation Server

pip install fastapi uvicorn googletrans==4.0.0-rc1

uvicorn translation_server:app --host 0.0.0.0 --port 8000