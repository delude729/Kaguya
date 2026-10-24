# Cursor resources

Original Chromium grab/grabbing cursor assets, distributed under the included CHROMIUM-LICENSE.txt.

Upstream sources:
- https://github.com/chromium/chromium/blob/main/ui/resources/cursors/hand_grab.cur
- https://github.com/chromium/chromium/blob/main/ui/resources/cursors/hand_grabbing.cur

Verified 2026-10-24 against the original pet application's installed Electron 42.11.18 executable. Cursor group resources 50254 (grab) and 50255 (grabbing) were extracted locally with tools/extract-original-cursors.ps1. Their reconstructed .cur files are byte-for-byte identical to the files in this directory (SHA-256 below).

- hand_grab.cur: 3DF4009E28E365E1666C868AEDE15239C75CBB6CF710CD691997B722C3EEA7F0
- hand_grabbing.cur: 22E823E71C106F338D42932C13C16E05A8310B3BDEC18A89CC5CA197408CF11A

Runtime uses the standard WinForms Cursor(file) loader. No custom cursor drawing, forced DPI scaling, or custom hotspot is applied. Pressing the mouse immediately selects grabbing, matching the original CSS :active rule. No Electron process, network access, or old project folder is needed at runtime.
