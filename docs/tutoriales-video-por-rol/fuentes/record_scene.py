#!/usr/bin/env python3
"""Graba una escena de capacitación del ERP con estado de sesión reutilizable."""

from __future__ import annotations

import argparse
import asyncio
import json
import os
from pathlib import Path

from playwright.async_api import Locator, Page, TimeoutError as PlaywrightTimeoutError, async_playwright


CURSOR_JS = Path("/home/vm/.codex/skills/erp-video-tutorials/scripts/cursor_overlay.js")


def ease_in_out(value: float) -> float:
    return value * value * (3 - 2 * value)


async def glide_mouse(page: Page, x1: float, y1: float, x2: float, y2: float, total_ms: int = 650) -> None:
    steps = max(20, min(45, total_ms // 18))
    delay = (total_ms / steps) / 1000
    for index in range(1, steps + 1):
        progress = ease_in_out(index / steps)
        await page.mouse.move(x1 + (x2 - x1) * progress, y1 + (y2 - y1) * progress)
        await asyncio.sleep(delay)


def locator_for(page: Page, step: dict) -> Locator:
    if step.get("selector"):
        return page.locator(step["selector"]).first
    if step.get("role") and step.get("name"):
        return page.get_by_role(step["role"], name=step["name"], exact=step.get("exact", True)).first
    if step.get("text"):
        return page.get_by_text(step["text"], exact=step.get("exact", True)).first
    raise ValueError(f"El paso no define un destino: {step}")


async def wait_until_stable(page: Page) -> None:
    try:
        await page.wait_for_load_state("networkidle", timeout=12_000)
    except PlaywrightTimeoutError:
        await page.wait_for_timeout(1_000)


async def record(args: argparse.Namespace) -> dict:
    scene = json.loads(Path(args.scene).read_text(encoding="utf-8"))
    credentials = json.loads(Path(args.credentials).read_text(encoding="utf-8"))
    credential = credentials[args.role_key]
    state_path = Path(args.storage_state) if args.storage_state else None

    errors: list[str] = []
    viewport = {"width": args.width, "height": args.height}
    Path(args.out).parent.mkdir(parents=True, exist_ok=True)
    if args.screenshot:
        Path(args.screenshot).parent.mkdir(parents=True, exist_ok=True)

    async with async_playwright() as playwright:
        browser = await playwright.chromium.launch(headless=not args.show)
        context_options = {
            "viewport": viewport,
            "record_video_dir": str(Path(args.out).parent),
            "record_video_size": viewport,
            "locale": "es-HN",
            "timezone_id": "America/Tegucigalpa",
            "color_scheme": "light",
        }
        if state_path and state_path.exists():
            context_options["storage_state"] = str(state_path)

        context = await browser.new_context(**context_options)
        await context.add_init_script(path=str(CURSOR_JS))
        page = await context.new_page()
        page.on("console", lambda message: errors.append(f"console:{message.type}:{message.text}") if message.type == "error" else None)
        page.on("pageerror", lambda exception: errors.append(f"pageerror:{exception}"))
        video = page.video
        last_x, last_y = args.width / 2, args.height / 2

        for step in scene["steps"]:
            action = step["action"]

            if action == "goto":
                await page.goto(step["url"], wait_until="domcontentloaded", timeout=30_000)
                await wait_until_stable(page)
            elif action == "wait":
                await page.wait_for_timeout(int(step.get("seconds", 1) * 1000))
            elif action in {"click", "hover"}:
                locator = locator_for(page, step)
                await locator.wait_for(state="visible", timeout=20_000)
                await locator.scroll_into_view_if_needed(timeout=10_000)
                box = await locator.bounding_box()
                if box is None:
                    raise RuntimeError(f"No se pudo ubicar el elemento: {step}")
                target_x = box["x"] + box["width"] / 2
                target_y = box["y"] + box["height"] / 2
                await glide_mouse(page, last_x, last_y, target_x, target_y, step.get("glide_ms", 650))
                last_x, last_y = target_x, target_y
                if action == "click":
                    await page.mouse.down()
                    await page.wait_for_timeout(120)
                    await page.mouse.up()
                    await page.wait_for_timeout(step.get("settle_ms", 900))
                    if step.get("wait_for_navigation", False):
                        await wait_until_stable(page)
                else:
                    await page.wait_for_timeout(step.get("hold_ms", 1_100))
            elif action == "type":
                value = step.get("text")
                if step.get("value_key"):
                    value = credential[step["value_key"]]
                if value is None:
                    raise ValueError(f"El paso de escritura no contiene valor: {step}")
                await page.keyboard.type(value, delay=step.get("char_delay_ms", 55))
            elif action == "scroll":
                await page.mouse.wheel(0, step.get("dy", 600))
                await page.wait_for_timeout(step.get("settle_ms", 700))
            else:
                raise ValueError(f"Acción desconocida: {action}")

            if step.get("hold_ms") and action != "hover":
                await page.wait_for_timeout(step["hold_ms"])

        if args.reject_login_redirect and "/login" in page.url:
            raise RuntimeError(f"La escena terminó en la pantalla de acceso: {scene['id']}")

        if args.screenshot:
            await page.screenshot(path=args.screenshot, full_page=False)
        if args.state_out:
            await context.storage_state(path=args.state_out)

        final_url = page.url
        await context.close()
        await video.save_as(args.out)
        await browser.close()

    return {
        "scene": scene["id"],
        "video": args.out,
        "final_url": final_url,
        "errors": errors,
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--scene", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--credentials", required=True)
    parser.add_argument("--role-key", required=True)
    parser.add_argument("--storage-state")
    parser.add_argument("--state-out")
    parser.add_argument("--screenshot")
    parser.add_argument("--width", type=int, default=1920)
    parser.add_argument("--height", type=int, default=1080)
    parser.add_argument("--show", action="store_true")
    parser.add_argument("--reject-login-redirect", action="store_true")
    args = parser.parse_args()
    print(json.dumps(asyncio.run(record(args)), ensure_ascii=False))


if __name__ == "__main__":
    main()
