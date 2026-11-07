# Backend API Contract Requirements

## Login Endpoint

### Endpoint
```
POST http://localhost:53607/api/user/login
```

### Request Headers
```
Content-Type: application/json
```

### Request Body
```json
{
  "loginName": "string (required, min: 3 chars)",
  "password": "string (required, min: 6 chars)"
}
```

**Example:**
```json
{
  "loginName": "user1",
  "password": "P@ssw0rd"
}
```

### Success Response (HTTP 200)
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "optional_refresh_token_string",
  "user": {
    "id": "user-id-123",
    "name": "User Name",
    "email": "user@example.com",
    "loginName": "user1",
    "roles": ["ADMIN", "USER"]
  },
  "expiresIn": 3600,
  "message": "Login successful"
}
```

**Response Fields:**
- `accessToken` (string, required) - JWT token for Bearer authentication
- `refreshToken` (string, optional) - Refresh token for token renewal
- `user` (object, optional) - User information object
  - `id` (string) - Unique user identifier
  - `name` (string) - User's full name
  - `email` (string) - User's email address
  - `loginName` (string) - User's login username
  - `roles` (array) - User's roles/permissions
- `expiresIn` (number, optional) - Token expiration time in seconds
- `message` (string, optional) - Success message

### Error Response (HTTP 400/401/422)
```json
{
  "statusCode": 400,
  "message": "Invalid credentials",
  "errors": {
    "loginName": "User not found",
    "password": "Incorrect password"
  },
  "timestamp": "2024-01-15T10:30:00Z",
  "path": "/api/user/login"
}
```

**Error Response Fields:**
- `statusCode` (number) - HTTP status code
- `message` (string) - General error message
- `errors` (object, optional) - Field-level validation errors
- `timestamp` (string, optional) - ISO 8601 timestamp
- `path` (string, optional) - Request path

**Common Status Codes:**
- `200` - Login successful
- `400` - Bad request (invalid input format)
- `401` - Unauthorized (invalid credentials)
- `422` - Validation error (validation failed)
- `500` - Internal server error

### HTTP Status Code Mapping

| Status | Scenario | Frontend Behavior |
|--------|----------|-------------------|
| 200 | Login successful | Store token, navigate to dashboard |
| 400 | Invalid request format | Show general error message |
| 401 | Invalid credentials | Show "Invalid username or password" |
| 403 | Account disabled/locked | Show "Access denied" |
| 404 | Service not found | Show "Service unavailable" |
| 422 | Validation errors | Show field-level errors |
| 500 | Server error | Show "Server error, try again" |
| Network Error | Connection failed | Show "Network error" |

## Authentication Usage

### Bearer Token Format
All authenticated requests must include the Authorization header:
```
Authorization: Bearer {accessToken}
```

**Example:**
```
GET /api/users/profile HTTP/1.1
Host: localhost:53607
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

### Token Validation
- Tokens are validated server-side before processing requests
- Invalid/expired tokens should return `401 Unauthorized`
- The frontend ErrorInterceptor will logout the user on 401

## Validation Error Format

### 422 Response with Field Errors
```json
{
  "statusCode": 422,
  "message": "Validation failed",
  "errors": {
    "loginName": [
      "Login name is required",
      "Login name must be at least 3 characters"
    ],
    "password": [
      "Password must contain at least one uppercase letter"
    ]
  }
}
```

**Notes:**
- `errors` object can have string or array values
- Each field can have multiple error messages
- Frontend will flatten and display these errors

## Public vs Protected Endpoints

### Public Endpoints (No Authentication)
These endpoints do NOT require an Authorization header:
- `POST /api/user/login`
- `POST /api/user/register`
- `POST /api/user/forgot-password`
- `POST /api/user/reset-password`

### Protected Endpoints
These endpoints REQUIRE an Authorization header with valid token:
- All other endpoints should require authentication

### Modifying Public Endpoints List
If your API has additional public endpoints, update the frontend:
```typescript
// src/app/core/interceptors/auth.interceptor.ts
private publicEndpoints = [
  '/api/user/login',
  '/api/user/register',
  '/api/user/forgot-password',
  '/api/health',  // Add new public endpoint here
];
```

## CORS Requirements

### Allowed Origins
The backend should allow requests from:
- `http://localhost:4200` (local development)
- `http://localhost:8080` (if running on different port)
- Production domain (when deployed)

### Allowed Methods
- GET
- POST
- PUT
- PATCH
- DELETE
- OPTIONS

### Allowed Headers
- Content-Type
- Authorization
- Accept-Language
- X-Requested-With

### Example CORS Configuration (Node.js/Express)
```javascript
app.use(cors({
  origin: ['http://localhost:4200', 'https://yourdomain.com'],
  credentials: true,
  methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'],
  allowedHeaders: ['Content-Type', 'Authorization', 'Accept-Language']
}));
```

## Rate Limiting Considerations

Consider implementing rate limiting to prevent abuse:
- Login attempts: 5 per minute per IP
- General API: 100 requests per minute per authenticated user
- Return `429 Too Many Requests` when exceeded

## Security Requirements

1. **HTTPS in Production** - All production traffic must use HTTPS
2. **Token Expiration** - Access tokens should expire (implement refresh tokens)
3. **Secure Cookies** - If using cookies, set HttpOnly and Secure flags
4. **Input Validation** - Validate all inputs server-side
5. **SQL Injection Prevention** - Use parameterized queries
6. **XSS Prevention** - Sanitize all outputs
7. **Password Security** - Hash passwords with bcrypt or similar
8. **Rate Limiting** - Implement rate limiting on login endpoints

## Testing with Frontend

### Test 1: Valid Login
```bash
curl -X POST http://localhost:53607/api/user/login \
  -H "Content-Type: application/json" \
  -d '{"loginName":"user1","password":"P@ssw0rd"}'
```

**Expected Response:**
```json
{
  "accessToken": "...",
  "user": {...}
}
```

### Test 2: Invalid Credentials
```bash
curl -X POST http://localhost:53607/api/user/login \
  -H "Content-Type: application/json" \
  -d '{"loginName":"user1","password":"wrongpassword"}'
```

**Expected Response (401):**
```json
{
  "statusCode": 401,
  "message": "Invalid credentials"
}
```

### Test 3: Validation Error
```bash
curl -X POST http://localhost:53607/api/user/login \
  -H "Content-Type: application/json" \
  -d '{"loginName":"ab","password":"short"}'
```

**Expected Response (422):**
```json
{
  "statusCode": 422,
  "message": "Validation failed",
  "errors": {
    "loginName": "Minimum 3 characters required",
    "password": "Minimum 6 characters required"
  }
}
```

## Sample Backend Implementation (Node.js/Express)

### Login Route
```javascript
router.post('/api/user/login', async (req, res) => {
  try {
    const { loginName, password } = req.body;
    
    // Validate input
    if (!loginName || !password) {
      return res.status(400).json({
        statusCode: 400,
        message: 'Missing required fields'
      });
    }
    
    // Find user
    const user = await User.findOne({ loginName });
    if (!user) {
      return res.status(401).json({
        statusCode: 401,
        message: 'Invalid credentials'
      });
    }
    
    // Verify password
    const validPassword = await bcrypt.compare(password, user.passwordHash);
    if (!validPassword) {
      return res.status(401).json({
        statusCode: 401,
        message: 'Invalid credentials'
      });
    }
    
    // Generate token
    const accessToken = jwt.sign(
      { id: user.id, loginName: user.loginName },
      process.env.JWT_SECRET,
      { expiresIn: '1h' }
    );
    
    // Return response
    res.json({
      accessToken,
      user: {
        id: user.id,
        name: user.name,
        email: user.email,
        loginName: user.loginName,
        roles: user.roles
      },
      expiresIn: 3600
    });
  } catch (error) {
    res.status(500).json({
      statusCode: 500,
      message: 'Internal server error'
    });
  }
});
```

## Deployment Checklist

- [ ] API running on production gateway URL
- [ ] HTTPS enabled for all endpoints
- [ ] CORS configured for production domain
- [ ] Rate limiting implemented
- [ ] Token expiration configured
- [ ] Error responses use standard format
- [ ] Validation errors returned with 422 status
- [ ] No sensitive data in error messages
- [ ] Logging configured for monitoring
- [ ] Database backups configured
