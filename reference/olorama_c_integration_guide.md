# Olorama C++ Integration Guide

Mirrored from <https://olorama.com/downloads/c_integration_guide.html> (linked
from the user guide's section 3.3, "Activation with C++/Java"). `olorama.com`
is blocked by this dev environment's network egress policy, so this is a copy
pasted in by hand rather than fetched — treat it as a secondary source, not a
live one, if the vendor ever revises the page.

The values here are the ones `tools/protocol.js` and `tools/activate-scent.js`
are built against: same `OUT,...` field layout and ranges, and the **UDP port
(5010)** that the main PDF manual doesn't print anywhere.

---

## Overview

Trigger Olorama scent devices from a C++ application via UDP messages.

**Network requirement:** your emitting PC/device must be connected to the
same Wi-Fi/router as the scent generator.

## UDP Message Specification

```
OUT,01,0100,1,02000,1000
```

| Field | Meaning |
|---|---|
| `01` | Position in the scent hub (which scent to activate) |
| `0100` | Intensity. Recommended range 0100–0300 (100 = min). Max value: 500 |
| `02000` | Fan activation time in milliseconds. Valid range 1000–9000 (do not exceed 9000) |
| Others | Internal parameters — do not modify |

**Default UDP port: `5010`**

## C++ Reference Implementation

```cpp
// olorama_udp.cpp
#include <cstdio>
#include <cstring>
#include <string>
#include <stdexcept>
#ifdef _WIN32
  #include <winsock2.h>
  #include <ws2tcpip.h>
  #pragma comment(lib, "ws2_32.lib")
  using socklen_t = int;
#else
  #include <arpa/inet.h>
  #include <netinet/in.h>
  #include <sys/socket.h>
  #include <unistd.h>
  #define INVALID_SOCKET (-1)
  #define SOCKET_ERROR   (-1)
  using SOCKET = int;
#endif

static void init_sockets(){
#ifdef _WIN32
  WSADATA wsa; if (WSAStartup(MAKEWORD(2,2), &wsa) != 0) throw std::runtime_error("WSAStartup failed");
#endif
}

static void close_socket(SOCKET s){
#ifdef _WIN32
  closesocket(s);
#else
  close(s);
#endif
}

static std::string format_message(int position, int intensity, int duration_ms){
  if (position < 1) position = 1; if (position > 99) position = 99;
  if (intensity < 100) intensity = 100; if (intensity > 500) intensity = 500;
  if (duration_ms < 1000) duration_ms = 1000; if (duration_ms > 9000) duration_ms = 9000;
  char pos[3]; std::snprintf(pos, sizeof(pos), "%02d", position);
  char inten[5]; std::snprintf(inten, sizeof(inten), "%04d", intensity);
  char dur[6]; std::snprintf(dur, sizeof(dur), "%05d", duration_ms);
  std::string msg = "OUT,"; msg += pos; msg += ","; msg += inten; msg += ",1,"; msg += dur; msg += ",1000";
  return msg;
}

static void send_udp(const std::string& ip, unsigned short port, const std::string& payload){
  SOCKET sock = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
  if (sock == INVALID_SOCKET) throw std::runtime_error("socket() failed");
  sockaddr_in addr{}; addr.sin_family = AF_INET; addr.sin_port = htons(port);
#ifdef _WIN32
  InetPtonA(AF_INET, ip.c_str(), &addr.sin_addr);
#else
  inet_pton(AF_INET, ip.c_str(), &addr.sin_addr);
#endif
  int sent = sendto(sock, payload.c_str(), (int)payload.size(), 0, (sockaddr*)&addr, (socklen_t)sizeof(addr));
  if (sent == SOCKET_ERROR) { close_socket(sock); throw std::runtime_error("sendto() failed"); }
  close_socket(sock);
}

int main(){
  try{
    init_sockets();
    const std::string targetIp = "192.168.1.100";
    const unsigned short port = 5010;
    const std::string msg = format_message(1, 200, 2000);
    send_udp(targetIp, port, msg);
    std::printf("Sent: %s\n", msg.c_str());
  }catch(const std::exception& e){
    std::fprintf(stderr, "Error: %s\n", e.what());
    return 1;
  }
  return 0;
}
```

Compile with `g++ -O2 -std=c++17 olorama_udp.cpp -o olorama_udp` or Visual Studio.

## Troubleshooting

- Ensure both your PC and the scent generator are on the same local network.
- Confirm UDP port 5010 is open and not blocked by any firewall.
- Check that the message format and comma placement exactly match the specification.

---

© 2025 Olorama Tech – Immersive scent technology for VR & Interactive Experiences

## Notes for this repo

- `format_message`'s position clamp is generic (1–99); our specific
  10-Scents Generator only has 10 cartridges, so `tools/protocol.js` clamps
  to 1–10 instead — that's a difference in the device, not the protocol.
- Everything else (intensity 100–500, fan time 1000–9000, the two fixed
  internal fields, the `OUT,pp,iiii,1,fffff,1000` layout) matches
  `tools/protocol.js` exactly.
- `tools/activate-scent.js` now defaults to this port (5010) instead of the
  earlier placeholder (8000).
