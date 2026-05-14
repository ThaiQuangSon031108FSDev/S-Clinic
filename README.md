# S-Clinic Management System 🏥

A modern, full-stack Clinic Management System built with **ASP.NET Core 8 MVC**, **Vue.js 3**, and **TailwindCSS**. Designed to streamline operations for Dermatology clinics, featuring real-time workflows via **SignalR** and a responsive UI.

![S-Clinic Banner](https://via.placeholder.com/1200x300.png?text=S-Clinic+Management+System)

## 🌟 Key Features

*   **Real-time Kanban Dashboard:** Receptionists can manage patient queues seamlessly. State changes (Check-in, Examined, Invoiced) are broadcasted instantly via SignalR.
*   **Doctor Workspace:** Efficient patient examination interface, including digital medical records, prescription management, and treatment session logging (Before/After photos).
*   **Treatment Packages (Gói Liệu Trình):** Fully integrated package management. Patients can buy packages (e.g., "Gói Trị Mụn 10 buổi") and schedule follow-ups without additional service fees.
*   **Finance & Invoicing:** Cashier module with automated invoice generation. Supports automatic VietQR generation for bank transfers.
*   **Patient Portal:** Patients can book appointments, track their purchased treatment packages, and view medical history.
*   **Admin Analytics:** Dashboard with KPI metrics, revenue charts, top-performing doctors, and staff management capabilities.

## 🚀 Technology Stack

*   **Backend:** ASP.NET Core 8 MVC, C#, Entity Framework Core
*   **Database:** SQL Server
*   **Real-time Communication:** SignalR
*   **Frontend:** Vue.js 3 (Composition API / Options API mixed), TailwindCSS v4
*   **Authentication:** ASP.NET Core Authentication Cookies & BCrypt Password Hashing
*   **Email Service:** MailKit (SMTP Integration)
*   **AI Integration:** Google Gemini API (for advanced clinical analysis features)

## ⚙️ Prerequisites

*   [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
*   SQL Server (LocalDB or Express)

## 🛠️ Setup & Installation

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/your-username/SClinic.git
    cd SClinic/SClinic
    ```

2.  **Configure the Database:**
    *   Open `appsettings.json` and ensure the `DefaultConnection` matches your local SQL Server instance.
    *   Default configuration: `"Server=.;Database=SClinicDb;Trusted_Connection=True;TrustServerCertificate=True;"`

3.  **Restore dependencies and build:**
    ```bash
    dotnet restore
    dotnet build
    ```

4.  **Run the application:**
    ```bash
    dotnet run
    ```
    *The application automatically seeds realistic demo data (Roles, Accounts, Patients, Doctors, Appointments, and Invoices) on the first run. No `dotnet ef database update` is required as it uses `EnsureCreatedAsync()`.*

## 🔐 Default Demo Accounts

The system will auto-generate the following accounts on startup. The password for **all** accounts is `Sclinic@123`.

| Role | Email |
| :--- | :--- |
| **Admin** | *Created manually or via DB seed* |
| **Doctor** | `bs.lethib@sclinic.vn` |
| **Receptionist** | `letan1@sclinic.vn` |
| **Patient** | `thaiquangson@gmail.com` |

## 📁 Project Structure

*   `/Controllers`: ASP.NET Core MVC Controllers (serving Views) and API Controllers (serving JSON for Vue components).
*   `/Models`: Entity Framework Core data models.
*   `/Services`: Business logic layer (TreatmentService, InvoiceService, EmailService).
*   `/Hubs`: SignalR Hubs (`ClinicHub.cs`) for real-time WebSockets.
*   `/Views`: Razor Views (`.cshtml`), many of which embed Vue 3 applications using custom delimiters `[[ ]]`.
*   `/wwwroot`: Static assets (CSS, JS, Images).

## 💡 Recent Updates

*   **Logic Enforcement:** Comprehensive server-side and client-side validation to prevent booking past time slots.
*   **Package Auto-Activation:** Invoices containing "Treatment Packages" automatically activate the package for the patient upon payment.
*   **Zero-Cost Invoice Handling:** Handled edge cases for 0 VND invoices (prepaid treatment sessions without extra medicine), automatically marking them as `Paid` to avoid Cashier bottlenecking.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.
