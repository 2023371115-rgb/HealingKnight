"""
Generador de sonidos de ciencia ficción para Healing Knight.

Cómo funciona (la "receta" de cada sonido):
  1. Un TONO que cambia de frecuencia (barrido): de f0 Hz a f1 Hz.
     - Agudo que baja  = "pew" de láser.     - Grave que baja = golpe pesado.
     - Que sube        = burbuja / carga.
  2. La FORMA de la onda le da el carácter:
     - "sine"   (seno):     limpio, suave, cristalino.
     - "square" (cuadrada): retro, de videojuego clásico.
     - "saw"    (sierra):   zumbido áspero, agresivo.
     - "tri"    (triángulo): entre seno y cuadrada, suave pero con cuerpo.
  3. RUIDO (opcional) mezclado: da "aire", chispa o suciedad (impactos, electricidad).
  4. Una ENVOLVENTE: el volumen arranca rápido y se apaga; "caida" más alta = se apaga más rápido.
  5. Extras: vibrato (tiembla), pulsos (burbujea), chispas (crujido eléctrico).

Uso (necesitas Python con numpy; y ffmpeg si quieres .ogg):
  python generar_sonidos.py                 -> genera TODOS en Assets/_Project/Audio/SFX
  python generar_sonidos.py Disparo_Rabia   -> genera solo ese

Para agregar uno nuevo: copia una entrada de SONIDOS, cámbiale el nombre y los números,
corre el script y escúchalo. Después asígnalo en Unity (arrastra el .ogg al campo del medicamento o enemigo).
"""
import os, sys, wave, shutil, subprocess
import numpy as np

SR = 44100
AQUI = os.path.dirname(os.path.abspath(__file__))
SALIDA = os.path.normpath(os.path.join(AQUI, "..", "..", "Assets", "_Project", "Audio", "SFX"))

# --------------------------------------------------------------------------------------------
# Lista de sonidos. Cada capa ("capas") se suma; luego se aplica la envolvente.
#   tono:   {"onda": "sine|square|saw|tri", "f0": Hz inicio, "f1": Hz final, "curva": 1 = recta,
#            <1 = cambia rápido al principio, >1 = cambia al final, "vol": 0..1,
#            "vibrato": [Hz, profundidad Hz] (opcional), "pulsos": Hz (opcional, burbujeo)}
#   ruido:  {"ruido": True, "suave": 0..1 (bajo = sordo, 1 = siseo), "vol": 0..1,
#            "chispas": 0..1 (opcional: deja solo chispazos sueltos, eléctrico)}
# --------------------------------------------------------------------------------------------
SONIDOS = {
    # ---------------- Disparos (cortos, menos de 0.2 s) ----------------
    "Disparo_Antibiotico":      {"dur": 0.16, "caida": 1.6, "capas": [
        {"onda": "square", "f0": 1800, "f1": 380, "curva": 0.6, "vol": 1}]},
    "Disparo_Antiviral":        {"dur": 0.18, "caida": 2.2, "capas": [
        {"onda": "sine", "f0": 2600, "f1": 1500, "curva": 0.5, "vol": 1},
        {"onda": "sine", "f0": 3900, "f1": 2250, "curva": 0.5, "vol": 0.6}]},
    "Disparo_Antiinflamatorio": {"dur": 0.20, "caida": 2.5, "ataque": 0.01, "capas": [
        {"ruido": True, "suave": 0.08, "vol": 0.6},
        {"onda": "tri", "f0": 700, "f1": 300, "curva": 0.7, "vol": 1}]},
    "Disparo_Antihistaminico":  {"dur": 0.20, "caida": 1.8, "capas": [
        {"onda": "sine", "f0": 1100, "f1": 600, "curva": 1, "vol": 1, "vibrato": [38, 160]}]},
    "Disparo_Antiparasitario":  {"dur": 0.18, "caida": 1.4, "capas": [
        {"onda": "saw", "f0": 420, "f1": 120, "curva": 0.8, "vol": 1},
        {"onda": "square", "f0": 840, "f1": 240, "curva": 0.8, "vol": 0.4}]},
    "Disparo_Mucolitico":       {"dur": 0.20, "caida": 1.5, "capas": [
        {"onda": "sine", "f0": 500, "f1": 1400, "curva": 1, "vol": 1, "pulsos": 45}]},
    "Disparo_Anticonvulsivo":   {"dur": 0.17, "caida": 1.8, "capas": [
        {"ruido": True, "suave": 1, "vol": 0.8, "chispas": 0.82},
        {"onda": "square", "f0": 3200, "f1": 1200, "curva": 0.4, "vol": 0.5}]},

    # ---------------- Impactos (un poco más graves/sucios que su disparo) ----------------
    "Impacto_Antibiotico":      {"dur": 0.14, "caida": 2.5, "capas": [
        {"onda": "square", "f0": 900, "f1": 200, "curva": 0.3, "vol": 1}, {"ruido": True, "suave": 0.25, "vol": 0.6}]},
    "Impacto_Antiviral":        {"dur": 0.16, "caida": 2.5, "capas": [
        {"onda": "sine", "f0": 2200, "f1": 900, "curva": 0.3, "vol": 1}, {"ruido": True, "suave": 0.5, "vol": 0.3}]},
    "Impacto_Antiinflamatorio": {"dur": 0.18, "caida": 3.0, "capas": [
        {"onda": "tri", "f0": 500, "f1": 180, "curva": 0.3, "vol": 1}, {"ruido": True, "suave": 0.06, "vol": 0.8}]},
    "Impacto_Antihistaminico":  {"dur": 0.15, "caida": 2.5, "capas": [
        {"onda": "sine", "f0": 1400, "f1": 500, "curva": 0.3, "vol": 1}, {"ruido": True, "suave": 0.2, "vol": 0.5}]},
    "Impacto_Antiparasitario":  {"dur": 0.20, "caida": 2.0, "capas": [
        {"onda": "saw", "f0": 260, "f1": 70, "curva": 0.3, "vol": 1}, {"ruido": True, "suave": 0.12, "vol": 0.7}]},
    "Impacto_Mucolitico":       {"dur": 0.16, "caida": 2.5, "capas": [
        {"onda": "sine", "f0": 700, "f1": 1300, "curva": 0.3, "vol": 1}, {"ruido": True, "suave": 0.1, "vol": 0.5}]},
    "Impacto_Anticonvulsivo":   {"dur": 0.15, "caida": 2.0, "capas": [
        {"onda": "square", "f0": 2800, "f1": 600, "curva": 0.3, "vol": 1}, {"ruido": True, "suave": 0.9, "vol": 0.9}]},

    # ---------------- Generales ----------------
    "Rebote_Ineficaz":          {"dur": 0.12, "caida": 4.0, "vol": 0.7, "capas": [
        {"onda": "square", "f0": 320, "f1": 300, "curva": 1, "vol": 1},
        {"onda": "sine", "f0": 1210, "f1": 1180, "curva": 1, "vol": 0.7},
        {"ruido": True, "suave": 0.3, "vol": 0.3}]},

    # ---------------- Ejemplos para enemigos (cópialos y ajústalos) ----------------
    "Enemigo_DisparoVirus":     {"dur": 0.22, "caida": 1.6, "capas": [
        {"onda": "tri", "f0": 300, "f1": 900, "curva": 0.6, "vol": 1, "vibrato": [20, 60]}]},
    "Enemigo_Muere":            {"dur": 0.35, "caida": 1.4, "capas": [
        {"onda": "saw", "f0": 600, "f1": 60, "curva": 0.5, "vol": 0.7}, {"ruido": True, "suave": 0.15, "vol": 0.8}]},
}

rng = np.random.default_rng(7)

def onda(tipo, fase):
    if tipo == "sine": return np.sin(fase)
    if tipo == "square": return np.sign(np.sin(fase)) * 0.6
    if tipo == "saw": return 2 * ((fase / (2 * np.pi)) % 1) - 1
    if tipo == "tri": return 2 * np.abs(2 * ((fase / (2 * np.pi)) % 1) - 1) - 1
    raise ValueError("onda desconocida: " + tipo)

def capa_tono(c, n):
    t = np.linspace(0, 1, n)
    f = c["f0"] + (c["f1"] - c["f0"]) * t ** c.get("curva", 1)
    if "vibrato" in c:
        hz, prof = c["vibrato"]
        f = f + prof * np.sin(2 * np.pi * hz * np.arange(n) / SR)
    s = onda(c["onda"], 2 * np.pi * np.cumsum(f) / SR)
    if "pulsos" in c:
        s = s * (np.sin(2 * np.pi * c["pulsos"] * np.arange(n) / SR) > -0.2)
    return s * c.get("vol", 1)

def capa_ruido(c, n):
    x = rng.uniform(-1, 1, n)
    a = c.get("suave", 1)
    if a < 1:  # filtro: menos agudos = ruido más sordo
        y = np.zeros(n)
        for i in range(1, n): y[i] = y[i - 1] + a * (x[i] - y[i - 1])
        x = y / (np.max(np.abs(y)) + 1e-9)
    if "chispas" in c:
        x = x * (rng.uniform(0, 1, n) > c["chispas"])
    return x * c.get("vol", 1)

def generar(nombre, d):
    n = int(SR * d["dur"])
    s = np.zeros(n)
    for c in d["capas"]:
        s += capa_ruido(c, n) if c.get("ruido") else capa_tono(c, n)
    t = np.linspace(0, 1, n)
    env = (1 - t) ** d.get("caida", 2)
    ataque = int(SR * d.get("ataque", 0.002))
    if ataque > 0: env[:ataque] *= np.linspace(0, 1, ataque)
    s = s * env
    s = s / (np.max(np.abs(s)) + 1e-9) * d.get("vol", 0.8)

    os.makedirs(SALIDA, exist_ok=True)
    wav = os.path.join(SALIDA, nombre + ".wav")
    with wave.open(wav, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((s * 32767).astype(np.int16).tobytes())

    # .ogg para el juego (los .wav van por Git LFS en este repo). Si no hay ffmpeg, se queda el .wav.
    if shutil.which("ffmpeg"):
        ogg = wav[:-4] + ".ogg"
        subprocess.run(["ffmpeg", "-loglevel", "error", "-y", "-i", wav, "-c:a", "libvorbis", "-q:a", "6", ogg], check=True)
        os.remove(wav)
        print("ok", os.path.basename(ogg))
    else:
        print("ok", os.path.basename(wav), "(instala ffmpeg para .ogg)")

if __name__ == "__main__":
    nombres = sys.argv[1:] or list(SONIDOS)
    for nombre in nombres:
        if nombre not in SONIDOS:
            print("No existe:", nombre); continue
        generar(nombre, SONIDOS[nombre])
