"""Compatibility entry point: import published lists unchanged."""
import runpy
from pathlib import Path
runpy.run_path(str(Path(__file__).with_name("import-published-solo.py")), run_name="__main__")
