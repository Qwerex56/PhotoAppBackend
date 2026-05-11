# Postman Demo Kit

This folder contains a Postman collection and environment for local demo and regression testing of the PhotoApp backend.

## What this solves

- No dependency on browser cookie sharing between Swagger tabs or service instances.
- Access tokens are stored in environment variables after login and refresh.
- Refresh uses the token from the login response body, so the flow is stable even when cookies are awkward to inspect.
- The collection is ordered for a live demo: register, verify, login, user management, media operations, cleanup.

## Files

- `photoapp-backend-demo.postman_collection.json`
- `photoapp-backend-local.postman_environment.json`
- `assets/demo-upload.png`

## How to use it

1. Start the local stack.
2. Import the environment file first.
3. Import the collection file.
4. Select the imported environment in Postman.
5. Run the `Auth` folder first.
6. After each register request, copy the verification token printed by `AuthService.API` into the matching `*_verification_token` variable.
7. Continue with login, profile, media, and cleanup requests.

## Notes

- The register requests generate unique demo emails automatically, so rerunning the collection will not hit duplicate-user conflicts.
- The upload request points to the included tiny PNG fixture. Replace it with any local `.jpg` or `.png` file if you want a real image.
- Password reset is included as an optional flow, but the current stub email service does not expose the reset token automatically.
- Role assignment uses the seeded role IDs from the development docs.
