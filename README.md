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

## 🧰 Tech Stack

- C# (.NET 7 or later)
- WinUI 3 (Windows App SDK)
- Async socket operations
- Custom ListView styling

## 📸 UI Preview

*(Coming soon: screenshots and/or video demo)*

## 🚧 Disclaimer

This tool is intended for **educational and diagnostic use** within your own network. Always scan responsibly.

---

> Inspired by curiosity. Refined by precision.
