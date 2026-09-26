"""Measures how fast each rumble motor of an XInput controller spins at each drive level.

Drives one motor at a time through XInput while recording a microphone, then reads the
rotation frequency of each step out of the recording: an eccentric-mass motor makes one
vibration cycle per revolution, so the strongest tone is the motor's speed (Hz x 60 = RPM).

Usage:
    python measure_motors.py [--device N] [--steps 20] [--hold 2.0] [--gap 1.0] [--list]

Writes to <repo>/.tmp/motors/<timestamp>/: recording.wav, steps.csv, spectrogram.png,
summary.txt. Nothing else must drive the controller while this runs (close x360ce).
"""
import argparse
import ctypes
import csv
import datetime
import os
import sys
import time
import wave

import numpy as np
import sounddevice as sd

RATE = 48000
FULL = 65535
# Where each motor's tone can be. The bands are apart so that the shell rattling on its surface, which
# the light motor sets off at the heavy motor's rate, is never taken for the light motor.
BAND = {"left": (8.0, 35.0), "right": (25.0, 120.0)}
PLOT_TOP = 120.0


class XINPUT_VIBRATION(ctypes.Structure):
    _fields_ = [("wLeftMotorSpeed", ctypes.c_ushort), ("wRightMotorSpeed", ctypes.c_ushort)]


class XINPUT_GAMEPAD(ctypes.Structure):
    _fields_ = [("wButtons", ctypes.c_ushort), ("bLeftTrigger", ctypes.c_ubyte), ("bRightTrigger", ctypes.c_ubyte),
                ("sThumbLX", ctypes.c_short), ("sThumbLY", ctypes.c_short), ("sThumbRX", ctypes.c_short), ("sThumbRY", ctypes.c_short)]


class XINPUT_STATE(ctypes.Structure):
    _fields_ = [("dwPacketNumber", ctypes.c_uint), ("Gamepad", XINPUT_GAMEPAD)]


def load_xinput():
    for name in ("xinput1_4.dll", "xinput1_3.dll", "xinput9_1_0.dll"):
        try:
            return ctypes.WinDLL(name)
        except OSError:
            continue
    raise SystemExit("No XInput library found.")


def connected_index(xi):
    state = XINPUT_STATE()
    for i in range(4):
        if xi.XInputGetState(i, ctypes.byref(state)) == 0:
            return i
    raise SystemExit("No XInput controller is connected.")


def set_motors(xi, index, left, right):
    v = XINPUT_VIBRATION(left, right)
    result = xi.XInputSetState(index, ctypes.byref(v))
    if result != 0:
        raise SystemExit("XInputSetState failed with %d." % result)


def tone_of(samples, band):
    """The strongest frequency in the band, and how far it stands above the band's median."""
    window = np.hanning(len(samples))
    spectrum = np.abs(np.fft.rfft(samples * window))
    freqs = np.fft.rfftfreq(len(samples), 1.0 / RATE)
    inside = (freqs >= band[0]) & (freqs <= band[1])
    power = spectrum[inside]
    if power.size == 0:
        return 0.0, 0.0
    peak = int(np.argmax(power))
    prominence = float(power[peak] / (np.median(power) + 1e-12))
    return float(freqs[inside][peak]), prominence


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("--device", type=int, default=None, help="sounddevice input index (see --list)")
    parser.add_argument("--steps", type=int, default=20, help="drive levels per motor between 0 and full")
    parser.add_argument("--hold", type=float, default=2.0, help="seconds each level is held")
    parser.add_argument("--gap", type=float, default=1.0, help="seconds of silence between levels")
    parser.add_argument("--list", action="store_true", help="list input devices and exit")
    args = parser.parse_args()
    if args.list:
        print(sd.query_devices())
        return
    repo = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
    out = os.path.join(repo, ".tmp", "motors", datetime.datetime.now().strftime("%Y%m%d-%H%M%S"))
    os.makedirs(out, exist_ok=True)

    xi = load_xinput()
    index = connected_index(xi)
    set_motors(xi, index, 0, 0)
    device = args.device if args.device is not None else sd.default.device[0]
    print("controller: XInput place %d; microphone: %s" % (index + 1, sd.query_devices(device)["name"]))

    levels = [round(FULL * k / args.steps) for k in range(1, args.steps + 1)]
    plan = [("left", level) for level in levels] + [("right", level) for level in levels]
    total = len(plan) * (args.hold + args.gap) + args.gap
    print("recording %.0f s: %d levels per motor, %.1f s each" % (total, args.steps, args.hold))

    chunks = []
    marks = []  # (motor, level, start_sample, end_sample)
    started = [None]

    def on_audio(indata, frames, time_info, status):
        if status:
            print("audio:", status, file=sys.stderr)
        chunks.append(indata[:, 0].copy())

    with sd.InputStream(device=device, channels=1, samplerate=RATE, dtype="float32", callback=on_audio):
        clock = time.perf_counter()
        time.sleep(args.gap)
        try:
            for motor, level in plan:
                left, right = (level, 0) if motor == "left" else (0, level)
                start = time.perf_counter() - clock
                set_motors(xi, index, left, right)
                time.sleep(args.hold)
                set_motors(xi, index, 0, 0)
                end = time.perf_counter() - clock
                marks.append((motor, level, start, end))
                print("  %s %3d%%" % (motor, round(100.0 * level / FULL)), end="\r", flush=True)
                time.sleep(args.gap)
        finally:
            set_motors(xi, index, 0, 0)
    print()

    audio = np.concatenate(chunks) if chunks else np.zeros(0, dtype="float32")
    with wave.open(os.path.join(out, "recording.wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((np.clip(audio, -1, 1) * 32767).astype("<i2").tobytes())

    rows = []
    for motor, level, start, end in marks:
        # The middle of the held step: past the spin-up, before the spin-down.
        a = int((start + 0.5) * RATE)
        b = int((end - 0.2) * RATE)
        segment = audio[a:b] if b <= len(audio) else audio[a:]
        hz, prominence = tone_of(segment, BAND[motor]) if len(segment) > RATE // 2 else (0.0, 0.0)
        rows.append((motor, level, round(100.0 * level / FULL), round(hz, 1), round(hz * 60), round(prominence, 1)))

    with open(os.path.join(out, "steps.csv"), "w", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["motor", "level", "percent", "hz", "rpm", "prominence"])
        writer.writerows(rows)

    lines = []
    for motor in ("left", "right"):
        mine = [r for r in rows if r[0] == motor]
        spinning = [r for r in mine if r[5] >= 3.0 and r[3] > 0]
        if spinning:
            lines.append("%s motor: starts at %d%%, %.1f Hz (%d RPM); at 100%%: %.1f Hz (%d RPM)" % (
                motor, spinning[0][2], spinning[0][3], spinning[0][4], mine[-1][3], mine[-1][4]))
        else:
            lines.append("%s motor: no clear tone found; check the microphone and level." % motor)
        for r in mine:
            lines.append("  %3d%%  %6.1f Hz  %5d RPM  prominence %.1f" % (r[2], r[3], r[4], r[5]))
    summary = "\n".join(lines)
    with open(os.path.join(out, "summary.txt"), "w") as f:
        f.write(summary + "\n")
    print(summary)

    try:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
        fig, ax = plt.subplots(figsize=(14, 6))
        ax.specgram(audio, NFFT=8192, Fs=RATE, noverlap=6144, cmap="magma", vmin=-120, vmax=-30)
        ax.set_ylim(0, PLOT_TOP)
        for motor, level, start, end in marks:
            ax.axvline(start, color="cyan" if motor == "left" else "lime", linewidth=0.5)
        ax.set_xlabel("seconds (cyan: left motor steps, green: right motor steps)")
        ax.set_ylabel("Hz")
        fig.tight_layout()
        fig.savefig(os.path.join(out, "spectrogram.png"), dpi=110)
    except Exception as ex:  # the picture is a convenience; the CSV is the result
        print("no spectrogram:", ex)
    print("written to", out)


if __name__ == "__main__":
    main()
