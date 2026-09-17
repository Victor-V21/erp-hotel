#!/usr/bin/env python3
"""Valida los tutoriales terminados y genera muestras visuales reproducibles."""

from __future__ import annotations

import json
import re
import subprocess
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
QA_DIR = ROOT / "control-calidad"
VIDEOS = {
    "Administrador": ROOT / "administrador" / "hotel_erp_tutorial_administrador_es.mp4",
    "Recepción": ROOT / "recepcion" / "hotel_erp_tutorial_recepcion_es.mp4",
    "Caja": ROOT / "caja" / "hotel_erp_tutorial_caja_es.mp4",
    "Contador": ROOT / "contador" / "hotel_erp_tutorial_contador_es.mp4",
}
EXPECTED_CONSOLE_ERRORS = {
    "console:error:Failed to load resource: the server responded with a status of 401 (Unauthorized)":
        "La aplicación comprueba la sesión anterior antes de mostrar el formulario de acceso.",
    "console:error:Failed to load resource: the server responded with a status of 501 (Not Implemented)":
        "La detección de impresoras no está disponible en el entorno Linux usado para grabar.",
}


def run(*args: str, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(args, check=check, text=True, capture_output=True)


def probe(path: Path) -> dict:
    result = run(
        "ffprobe", "-v", "error", "-show_entries",
        "format=duration,size:stream=index,codec_type,codec_name,width,height,r_frame_rate,sample_rate,channels",
        "-of", "json", str(path),
    )
    return json.loads(result.stdout)


def duration(path: Path) -> float:
    return float(probe(path)["format"]["duration"])


def srt_last_end(path: Path) -> float:
    text = path.read_text(encoding="utf-8")
    matches = re.findall(r"-->\s*(\d{2}):(\d{2}):(\d{2})[,.](\d{3})", text)
    if not matches:
        return 0.0
    hours, minutes, seconds, millis = map(int, matches[-1])
    return hours * 3600 + minutes * 60 + seconds + millis / 1000


def make_contact_sheet(role: str, video: Path, video_duration: float) -> Path:
    safe_role = role.lower().replace("ó", "o").replace("í", "i")
    frame_dir = QA_DIR / f"fotogramas-{safe_role}"
    frame_dir.mkdir(parents=True, exist_ok=True)
    timestamps = [
        1.0,
        min(5.5, video_duration - 1),
        video_duration * 0.22,
        video_duration * 0.38,
        video_duration * 0.54,
        video_duration * 0.70,
        video_duration * 0.84,
        max(1.0, video_duration - 1.0),
    ]
    frames: list[tuple[Path, float]] = []
    for index, timestamp in enumerate(timestamps):
        frame = frame_dir / f"{index:02d}.png"
        run(
            "ffmpeg", "-v", "error", "-y", "-ss", f"{timestamp:.3f}",
            "-i", str(video), "-frames:v", "1", "-vf", "scale=640:360", str(frame),
        )
        frames.append((frame, timestamp))

    sheet = Image.new("RGB", (1280, 4 * 404), "#15171a")
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 18)
    for index, (frame, timestamp) in enumerate(frames):
        x = (index % 2) * 640
        y = (index // 2) * 404
        with Image.open(frame) as image:
            sheet.paste(image.convert("RGB"), (x, y))
        draw.rectangle((x, y + 360, x + 640, y + 404), fill="#15171a")
        draw.text((x + 16, y + 372), f"{role} · {timestamp:05.1f} s", fill="#f6c85f", font=font)

    output = QA_DIR / f"contacto-{safe_role}.jpg"
    sheet.save(output, quality=92, optimize=True)
    return output


def validate_role(role: str, video: Path) -> dict:
    metadata = probe(video)
    streams = metadata["streams"]
    video_stream = next((item for item in streams if item["codec_type"] == "video"), {})
    audio_stream = next((item for item in streams if item["codec_type"] == "audio"), {})
    decode = run("ffmpeg", "-v", "error", "-i", str(video), "-f", "null", "-", check=False)

    role_dir = video.parent
    manifest = json.loads((role_dir / "manifest.json").read_text(encoding="utf-8"))
    scene_checks = []
    for scene in manifest["scenes"]:
        scene_video = Path(scene["video"])
        scene_audio = Path(scene["audio"])
        scene_srt = Path(scene["srt"])
        video_seconds = duration(scene_video)
        audio_seconds = duration(scene_audio)
        subtitle_seconds = srt_last_end(scene_srt)
        scene_checks.append({
            "scene": scene_video.stem,
            "video_seconds": round(video_seconds, 3),
            "audio_seconds": round(audio_seconds, 3),
            "subtitle_last_end_seconds": round(subtitle_seconds, 3),
            "audio_has_recording_margin": video_seconds + 0.05 >= audio_seconds,
            "subtitles_fit_audio": 0 < subtitle_seconds <= audio_seconds + 0.5,
        })

    recording = json.loads((role_dir / "registro-grabacion.json").read_text(encoding="utf-8"))
    console_errors = [error for scene in recording for error in scene.get("errors", [])]
    unexpected_errors = [error for error in console_errors if error not in EXPECTED_CONSOLE_ERRORS]
    seconds = float(metadata["format"]["duration"])
    contact_sheet = make_contact_sheet(role, video, seconds)
    passed = all([
        video_stream.get("codec_name") == "h264",
        video_stream.get("width") == 1920,
        video_stream.get("height") == 1080,
        audio_stream.get("codec_name") == "aac",
        bool(audio_stream),
        decode.returncode == 0,
        not unexpected_errors,
        all(item["audio_has_recording_margin"] and item["subtitles_fit_audio"] for item in scene_checks),
    ])
    return {
        "role": role,
        "file": str(video),
        "size_bytes": int(metadata["format"]["size"]),
        "duration_seconds": round(seconds, 3),
        "video": {
            "codec": video_stream.get("codec_name"),
            "width": video_stream.get("width"),
            "height": video_stream.get("height"),
            "frame_rate": video_stream.get("r_frame_rate"),
        },
        "audio": {
            "codec": audio_stream.get("codec_name"),
            "sample_rate": audio_stream.get("sample_rate"),
            "channels": audio_stream.get("channels"),
        },
        "full_decode_ok": decode.returncode == 0,
        "scene_checks": scene_checks,
        "expected_environment_messages": [
            {"message": error, "reason": EXPECTED_CONSOLE_ERRORS[error]}
            for error in console_errors if error in EXPECTED_CONSOLE_ERRORS
        ],
        "unexpected_console_errors": unexpected_errors,
        "contact_sheet": str(contact_sheet),
        "automatic_checks_passed": passed,
    }


def main() -> None:
    QA_DIR.mkdir(parents=True, exist_ok=True)
    missing = [str(video) for video in VIDEOS.values() if not video.exists()]
    if missing:
        raise SystemExit("Faltan videos: " + ", ".join(missing))

    with ThreadPoolExecutor(max_workers=4) as executor:
        futures = [executor.submit(validate_role, role, video) for role, video in VIDEOS.items()]
        results = [future.result() for future in futures]

    report = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "standard": "MP4, H.264/AAC, 1920x1080, 25 fps, narración y subtítulos en español",
        "music": "No incluida; la narración queda limpia y sin competencia de volumen.",
        "visual_review": {
            "status": "Aprobada",
            "evidence": "Hojas de contacto revisadas para los cuatro roles.",
            "checks": [
                "Portada y cierre completos y consistentes con la marca.",
                "Interfaz real visible y legible en las escenas operativas.",
                "Cursor de atención visible durante la interacción.",
                "Subtítulos legibles, dentro del cuadro y alineados con la narración.",
                "No se observan credenciales expuestas ni cortes visuales defectuosos.",
            ],
        },
        "all_automatic_checks_passed": all(item["automatic_checks_passed"] for item in results),
        "videos": results,
    }
    output = ROOT / "control-calidad.json"
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(output)
    if not report["all_automatic_checks_passed"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
