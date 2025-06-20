# BeaconScan

**BeaconScan** began as an exploration of WinUI 3 capabilities, particularly to experiment with a custom-tailored `ListView` component. Over time, it evolved into a practical network scanning tool focused on discovering active devices within a local network.

## ✨ Features

- Detects active IP addresses on your local network.
- Displays IPs and their open ports (predefined set).
- Automatically opens interfaces on ports commonly used by:
  - HTTP (80)
  - HTTPS (443)
  - Webcams
  - Remote Desktop Protocol (RDP)
- Optional SYN-based scanning for proactive IP discovery.

## 🔧 Technical Notes

BeaconScan is **not** a replacement for tools like `nmap`—it does **not** rely on packet capture (`pcap`) libraries. Instead, it uses lightweight probing with a predefined list of ports. If you need to scan a wider range, you can modify the source code under the `PortScan` module.

# 🧩 Requirements

To build and run BeaconScan, you’ll need:

- **Visual Studio 2022** (latest release recommended)
- **.NET 8 SDK**
- Windows 10/11 with support for WinUI 3 (App SDK runtime)

Make sure the `Windows App SDK` and `Desktop Development with C++` workloads are installed via the Visual Studio Installer, as WinUI 3 depends on them.


## 🧰 Tech Stack

- C# (.NET 7 or later)
- WinUI 3 (Windows App SDK)
- Async socket operations
- Custom ListView styling

## 📸 UI Preview

## 📸 UI Preview

![Main Screen](https://github.com/Taguapire/BeaconScan/blob/master/resources/Principal_Screen.png)

The main interface displays discovered devices in a custom `ListView`, showing IP addresses and detected open ports. It's designed for clarity and speed, with a layout that feels both modern and efficient.


## 🚧 Disclaimer

This tool is intended for **educational and diagnostic use** within your own network. Always scan responsibly.

---

> Inspired by curiosity. Refined by precision.
