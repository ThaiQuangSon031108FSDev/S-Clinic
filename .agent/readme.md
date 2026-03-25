# ADVANCED ARCHITECTURE & DEVOPS REQUIREMENTS (CRITICAL)
To ensure this project meets enterprise-grade standards, you MUST implement the following advanced features:

**1. Authentication & Security (JWT):**
- Since the frontend utilizes Vue 3 via CDN within Razor Views, the system will rely heavily on AJAX/Fetch API calls.
- Configure **JWT (JSON Web Token) Authentication** in `Program.cs`. 
- Implement an `AuthController` with a `Login` endpoint that validates credentials and returns a JWT token containing User Claims (AccountId, RoleId).
- Protect sensitive API controllers (e.g., `Api/InvoicesController`) using `[Authorize]` attributes.

**2. Custom Validation:**
- Do not rely solely on basic Data Annotations. Create at least 2 **Custom Validation Attributes**:
  - `[FutureDateOnly]`: To ensure `WorkDate` in `DoctorSchedules` or `Appointments` cannot be set in the past.
  - `[ValidPhoneFormat]`: To enforce a specific Regex format for Vietnamese phone numbers in the `Patient` entity.

**3. Fluent API Enhancements:**
- Ensure `ApplicationDbContext.cs` uses `Fluent API` extensively. Besides `decimal(18,2)`, implement explicit configuration for `DeleteBehavior.Restrict` on critical Foreign Keys (e.g., Do not allow deleting a `Medicine` if it's already referenced in `InvoiceDetails`).

**4. CI/CD & Docker (DevOps):**
- Automatically generate a standard `Dockerfile` for this .NET 8 application.
- Generate a GitHub Actions workflow file (`.github/workflows/dotnet-cicd.yml`) that restores dependencies, builds the project, and runs tests whenever a push is made to the `main` branch.