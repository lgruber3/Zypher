from fastapi import FastAPI
from googletrans import Translator
from pydantic import BaseModel

app = FastAPI()
translator = Translator()

class TranslationRequest(BaseModel):
    text: str
    target_lang: str = "de"  

@app.post("/translate")
def translate_text(request: TranslationRequest):
    translated = translator.translate(request.text, dest=request.target_lang)
    return {"translated_text": translated.text}
