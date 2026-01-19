# chatapp_kriptoloji — Secure Chat (Client/Server)

A C# client-server chat application focused on **secure communication** and **applied cryptography**.

---

## Why this project?

The goal is to practice real-world security fundamentals:
- Secure data transmission over networks
- Encryption / decryption flows
- Key handling and message integrity mindset
- Building a correct client-server communication model

This project is intentionally **backend + security oriented** (UI is minimal).

---

## Architecture

- **Server:** TCP listener, manages connected clients, routes messages
- **Client:** TCP client, sends/receives messages
- **Communication model:** Client ↔ Server (message relay)

---

## Cryptography Focus

- Encrypt message payloads before sending
- Decrypt payloads on receive
- Treat key handling as a first-class concern

> Notes: Exact algorithms and modes are documented in code and can be expanded (AES modes, RSA key exchange, etc.) depending on implementation.

---

## Tech Stack

- **Language:** C#
- **Networking:** TCP Sockets
- **Security:** .NET Cryptography APIs
- **Paradigm:** Client-Server model

---

## Features

- Multi-client messaging via server
- Encrypted message transfer
- Console-based logging and debugging support

---

## What I learned

- Socket-based communication patterns
- Practical encryption flow integration
- Why key management is the real challenge
- Writing clearer, safer code in networked programs

---

## Run

1. Start the **server**
2. Start one or more **clients**
3. Connect clients to server
4. Send messages (payload is encrypted)

---

## Roadmap (Optional)

- Add secure key exchange (e.g., RSA handshake → AES session key)
- Add message integrity (HMAC)
- Add replay protection / nonce handling
