from transformers import AutoTokenizer, AutoModel
from optimum.exporters.onnx import main_export
from pathlib import Path
# Script pour exporter un modèle BERT multilingue en ONNX
# Nom du modèle BERT multilingue
model_name = "google-bert/bert-base-multilingual-cased"
# Export vers un répertoire local
onnx_dir = Path("onnx-bert-multilingual")
print(f"Chargement du modèle {model_name}...")

# Lance l'export
main_export(
    model_name_or_path=model_name,
    output=onnx_dir,
    task="feature-extraction", 
    opset=17
)

print(f"Modèle ONNX exporté vers {onnx_dir}")
