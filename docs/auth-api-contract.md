# LearnQ Authentication & Authorization API Contract (Spring Boot Backend)

This document defines the backend authentication and authorization REST API specification required by the LearnQ .NET frontend.

---

## 1. Overview & General Conventions

- **Base URL**: Configured via `Api:BaseUrl` (e.g. `http://localhost:8888/`).
- **Security Standard**: OAuth2 Resource Server issuing JSON Web Tokens (JWT).
- **Public vs Protected Endpoints**:
  - Public (`permitAll`): `POST /api/clients/register`, `POST /api/auth/login`, `POST /api/auth/refresh`.
  - Protected (`authenticated`): All other endpoints require a valid JWT Bearer token in the `Authorization: Bearer <accessToken>` header.
- **Error Response Standard**: RFC 7807 `ProblemDetail` matching `online.learnq.api.common.exception.GlobalExceptionHandler`:
  ```json
  {
    "type": "about:blank",
    "title": "Bad Request",
    "status": 400,
    "detail": "Validation Failed",
    "instance": "/api/auth/login",
    "code": "VALIDATION_FAILED",
    "errors": {
      "identifier": ["Identifier is required."]
    }
  }
  ```
- **Error Codes**:
  - `VALIDATION_FAILED` (400)
  - `UNAUTHORIZED` (401)
  - `FORBIDDEN` (403)
  - `RESOURCE_NOT_FOUND` (404)
  - `RESOURCE_CONFLICT` (409)
  - `INTERNAL_ERROR` (500)

---

## 2. JWT & Token Conventions

### 2.1 Token Lifetimes
- **Access Token**: 15 minutes (short-lived).
- **Refresh Token (Standard Session)**: 8 hours.
- **Refresh Token (Remember Me)**: 30 days.

### 2.2 Access Token Payload Claims
The access token must contain standard JWT claims:
- `sub`: Unique user ID (UUID or numeric string).
- `email`: User's primary email address.
- `role`: Role string (`STUDENT`, `TEACHER`, or omitted/null if not yet selected).
- `exp`: Expiration timestamp (seconds since Unix epoch).
- `iat`: Issued at timestamp.

### 2.3 Refresh Token Rotation
- Every call to `POST /api/auth/refresh` must revoke the provided refresh token and issue a newly minted refresh token along with the new access token.
- Reuse detection: If an already-invalidated refresh token is presented, the backend must immediately revoke all tokens in the corresponding token family (invalidating the user's session).

---

## 3. Endpoint Specifications

### 3.1 POST /api/clients/register (Existing)
Registers a new user account with initial status `PENDING`.
- **Access**: Public (`permitAll`)
- **Request Body**:
  ```json
  {
    "givenName": "Jane",
    "familyName": "Doe",
    "displayName": "Jane Doe",
    "email": "jane.doe@example.com",
    "password": "SecurePassword123!",
    "termsAccepted": true,
    "privacyAccepted": true
  }
  ```
- **Success Response**: `201 Created`
  ```json
  {
    "id": 1,
    "givenName": "Jane",
    "familyName": "Doe",
    "displayName": "Jane Doe",
    "email": "jane.doe@example.com",
    "status": "PENDING"
  }
  ```

---

### 3.2 POST /api/auth/login
Authenticates a user via email or username and issues an access/refresh token pair.
- **Access**: Public (`permitAll`)
- **Request Body**:
  ```json
  {
    "identifier": "jane.doe@example.com",
    "password": "SecurePassword123!",
    "rememberMe": false
  }
  ```
- **Success Response**: `200 OK`
  ```json
  {
    "accessToken": "eyJhbGciOi...",
    "refreshToken": "d8f3b2a1...",
    "expiresAt": "2026-11-01T20:15:00Z"
  }
  ```
- **Error Responses**:
  - `401 Unauthorized` with `code: "UNAUTHORIZED"`, `detail: "Invalid email/username or password"` (generic error message to prevent account enumeration).
  - `429 Too Many Requests` when account is locked or rate limit exceeded.

---

### 3.3 POST /api/auth/refresh
Exchanges a valid refresh token for a fresh token pair.
- **Access**: Public (`permitAll`)
- **Request Body**:
  ```json
  {
    "refreshToken": "d8f3b2a1..."
  }
  ```
- **Success Response**: `200 OK`
  ```json
  {
    "accessToken": "eyJhbGciOi...",
    "refreshToken": "e9a4c3b2...",
    "expiresAt": "2026-11-01T20:30:00Z"
  }
  ```
- **Error Responses**:
  - `401 Unauthorized` with `code: "UNAUTHORIZED"` if refresh token is expired, revoked, or invalid.

---

### 3.4 POST /api/auth/logout
Revokes the specified refresh token and invalidates active token sessions.
- **Access**: Authenticated (`Authorization: Bearer <accessToken>`)
- **Request Body**:
  ```json
  {
    "refreshToken": "e9a4c3b2..."
  }
  ```
- **Success Response**: `204 No Content` or `200 OK`

---

### 3.5 GET /api/auth/me
Retrieves current user identity, role, email verification status, and onboarding progress.
- **Access**: Authenticated (`Authorization: Bearer <accessToken>`)
- **Success Response**: `200 OK`
  ```json
  {
    "id": "usr_12345",
    "email": "jane.doe@example.com",
    "displayName": "Jane Doe",
    "role": "STUDENT",
    "emailVerified": false,
    "onboardingCompleted": false,
    "onboardingStatus": "ROLE_SELECTED"
  }
  ```
  *(Note: `role` is `null` if user has not yet completed the role selection step).*

---

### 3.6 POST /api/auth/verify-email
Verifies user email using a 6-digit confirmation code.
- **Access**: Authenticated (`Authorization: Bearer <accessToken>`)
- **Request Body**:
  ```json
  {
    "code": "123456"
  }
  ```
- **Validation & Security**:
  - Code format: 6 numeric digits (`^\d{6}$`).
  - Codes must be stored hashed at rest (e.g. SHA-256 with salt / bcrypt).
  - Single-use and expires after 10 minutes.
  - Maximum attempt lockout (e.g. 5 failed attempts locks out the code).
- **Success Response**: `200 OK` or `204 No Content`
- **Error Responses**:
  - `400 Bad Request` with `code: "VALIDATION_FAILED"`, `detail: "Invalid or expired verification code."`

---

### 3.7 POST /api/auth/resend-verification
Triggers delivery of a new 6-digit email verification code.
- **Access**: Authenticated (`Authorization: Bearer <accessToken>`)
- **Success Response**: `200 OK`
  ```json
  {
    "retryAfterSeconds": 60
  }
  ```
- **Error Responses**:
  - `429 Too Many Requests` with `retryAfterSeconds` property and `code: "VALIDATION_FAILED"` (or `TOO_MANY_REQUESTS`) if requested within cooldown period.
