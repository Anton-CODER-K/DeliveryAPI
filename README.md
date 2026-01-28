# Delivery API

Backend API for a delivery service built with **ASP.NET Core** and **PostgreSQL**.  
The project focuses on authentication, verification flows, and clean backend architecture.

---

## 🚀 Features

- Phone-based authentication (SMS flow)
- SMS verification with expiration and attempt limits
- Resend cooldown and daily limits
- Verification sessions for secure flows
- JWT access token generation
- Clean separation of layers (API / Application / Infrastructure)
- Transactional database operations

---

## 🔐 Authentication Flow (SMS)

1. Client sends phone number to `/auth/start`
2. Server:
   - normalizes and validates phone number
   - generates verification code
   - stores hashed code with expiration
   - applies resend cooldown and daily limits
3. Client submits code to `/auth/verify`
4. Server:
   - validates code and expiration
   - tracks remaining attempts
   - marks code as used
   - generates verification session / token

---

## 🧱 Project Structure


---

## 🌿 Branches

- `master` — stable version (ready to review)
- `develop` — active development branch

---

## 🛠 Tech Stack

- ASP.NET Core
- PostgreSQL
- Npgsql
- JWT Authentication
- BCrypt / SHA256 (hashing)
- Git / GitHub

---

## 📌 Notes

This project is developed as part of a backend portfolio and demonstrates:
- real-world authentication logic
- database-driven validation
- transactional consistency
- clean and readable code structure
