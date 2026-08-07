# VCardix - vCard, CSV and JSON Contact Manager Software

[![GitHub downloads](https://img.shields.io/github/downloads/turkaysoft/vcardix/total?style=flat&color=1a893c&label=Downloads)](https://github.com/turkaysoft/vcardix/releases)
[![GitHub stars](https://img.shields.io/github/stars/turkaysoft/vcardix?style=flat&color=0062cc&label=Stars)](https://github.com/turkaysoft/vcardix/stargazers)
[![GitHub release](https://img.shields.io/github/v/release/turkaysoft/vcardix?style=flat&color=5a32a3&label=Latest%20Release)](https://github.com/turkaysoft/vcardix/releases/latest)
[![Platform](https://img.shields.io/badge/platform-Windows-b31d28?style=flat&label=Platform)](https://github.com/turkaysoft/vcardix)

**VCardix** is a high-performance **contact management and editing software** developed by **Eray Türkay**. Designed with a modern algorithm that ranks it among the best in its class, VCardix allows you to manage, edit, and convert your digital contacts across vCard, CSV, and JSON formats with surgical precision. It is the ultimate tool for users who need a fast and reliable way to handle complex contact data.

---

### Donate
You can support this project by making a donation to help ensure its sustainability and the development of new features.

[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-Donate-0a6628?style=flat&logo=buy-me-a-coffee&logoColor=white)](https://buymeacoffee.com/turkaysoft)

---

## Key Features

* **Privacy First:** Your data stays on your machine; no information is transferred to external servers.
* **Pure Performance:** Developed exclusively in **C# and .NET Framework** with no external libraries or dependencies.
* **Portable:** No installation required. Just download it, extract all files from the ZIP, select the appropriate architecture, and run it.
* **Universal vCard Standard Support:** Full compatibility with **vCard 4.0 (RFC 6350), vCard 3.0 (RFC 2426), and vCard 2.1** standards, ensuring seamless interoperability with all major contact management platforms.
* **Intelligent Change Tracking:** A data change tracking system monitors all input fields and warns you about unsaved changes before closing — preventing accidental data loss.
* **Autosave System:** When switching between contacts, memory data is automatically updated. The system also prompts for unsaved changes when closing the form, and an intelligent field comparison mechanism preserves unchanged fields to improve editing performance.
* **E.164 Phone Number Normalization:** Phone numbers are automatically normalized to **E.164 international format**, and vCard 4.0 TEL URI formatting is generated automatically for maximum compatibility.
* **Smart Email Distribution:** Automatic distribution system for email fields based on TYPE parameters, respecting **PREF, WORK, and INTERNET** labels for proper email categorization.
* **MIME Type Detection:** Photo files are automatically identified using signature-based detection, recognizing **PNG, JPEG, GIF, BMP, and TIFF** formats without relying on file extensions.
* **Improved CSV Compatibility:** CSV import and export has been re-engineered with enhanced compatibility based on the **Google Contacts CSV format** standard.
* **Refined JSON Import:** The JSON import process has been improved and refined to produce cleaner and more accurate results from structured contact data.
* **Modern UI:** Clean, intuitive interface compatible with Windows 11 design language, featuring Light, Dark, and System themes.
* **Multilingual:** It supports 15 different languages, primarily English. You can access the supported languages here: [Supported Languages](https://github.com/turkaysoft/vcardix/discussions/1)
* **Built-in Update Mechanism:** It features a built-in smart update mechanism developed specifically by **Türkaysoft**.

---

## Interface Preview

<img width="1010" height="633" alt="VCardix UI" src="https://github.com/user-attachments/assets/740d0a4b-8efd-4ee3-8abb-8d6792f1948a" />

---

## Getting Started

1.  Navigate to the **[Releases](https://github.com/turkaysoft/vcardix/releases/latest)** page.
2.  Download the latest ZIP file.
3.  **Extract all files from the ZIP** (Important: Application requires all folder contents to run correctly).
4.  Launch the executable corresponding to your architecture:
    * `VCardix_x64.exe`: For standard 64-bit Intel/AMD systems.
    * `VCardix_arm64.exe`: For ARM-based devices like Surface Pro.

---

## Translation Support

* **Translation Support:** Community-driven localization via the official [Translation Guide](https://github.com/turkaysoft/vcardix/discussions/1).

---

## System Requirements

| Feature | Minimum Requirements | Recommended Requirements |
| :--- | :--- | :--- |
| **OS** | Windows 10 22H2 x64 | Windows 11 25H2 x64 |
| **CPU** | x64 or ARM64 | x64 or ARM64 |
| **RAM** | 50 MB Free RAM | 100 MB Free RAM |
| **.NET** | .NET Framework 4.8.1 | .NET Framework 4.8.1 |

---

## Shortcut Keys

| Shortcut | Action |
|--|--|
| `F1` | Light Theme |
| `F2` | Dark Theme |
| `F3` | System Theme |
| `F4` | Starting With: Windowed |
| `F5` | Starting With: Full Screen |
| `F11` | Check Updates |
| `F12` | About |
| `CTRL + Alt + D` | Donate Page |
| `CTRL + N` | Import File |
| `CTRL + S` | Export File |
| `CTRL + 2` | vCard 2.1 |
| `CTRL + 3` | vCard 3.0 |
| `CTRL + 4` | vCard 4.0 |
| `CTRL + Shift + 1` | Sorting: Full Name |
| `CTRL + Shift + 2` | Sorting: First Name |
| `CTRL + Shift + 3` | Sorting: Last Name |
| `CTRL + Shift + 4` | Sorting: Mobile Phone |

---

## Security

* **Zero Data Export Policy:** Your privacy is our priority; no data leaves your machine.
* **No Dependencies:** Developed entirely from scratch using its own source code, there are no risks from security vulnerabilities in third-party libraries.
* **Open Source:** All source code for the program is open and can be reviewed by anyone.

---

## License

This software is offered free of charge as part of the **Türkaysoft solutions package** and is protected under the [**MIT License**](https://github.com/turkaysoft/vcardix?tab=MIT-1-ov-file).
