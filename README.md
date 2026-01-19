# BookNest - Online Library System

BookNest is a robust Online Library and E-Commerce platform built with **ASP.NET Core 9.0 MVC**. It facilitates a seamless borrowing and purchasing experience for students, while providing powerful management tools for librarians and administrators.

The system features a three-tier role-based access control (Student, Librarian, Admin), custom session-based authentication, and a modern responsive UI built with Bootstrap 5.

## 📋 Table of Contents
- [Features](#-features)
- [Technology Stack](#-technology-stack)
- [Architecture](#-architecture)
- [Prerequisites](#-prerequisites)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Project Structure](#-project-structure)

## ✨ Features

### 🎓 Student (User) Features
Accessible via the personalized Student Sidebar:
- **Dashboard**: Overview of account activity.
- **Browse Books**: View available books with filtering by category.
- **Read Book Samples**: Integrated PDF viewer to read book samples directly in the browser (tracks reading progress).
- **My Cart**: Manage selected books for borrowing or purchasing.
- **My Orders**: Track status of current and past orders (Pending, Approved, Shipped, etc.).
- **My Refunds**: View status of requested refunds and refund history.
- **Profile**: Update personal details, contact info, and password.
- **Notifications**: Real-time alerts for order updates and system messages.

### 📚 Librarian Features
Accessible via the Librarian Panel:
- **Dashboard**: Quick stats on orders and returns.
- **Manage Books**: Add, edit, and remove books from the library inventory.
- **Categories**: View and manage book categories.
- **All Orders**: Process incoming student orders (Approve/Reject).
- **Return Requests**: Review and approve book return requests.
- **Refund History**: Track all processed refunds.

### 👨‍💼 Admin Features
Accessible via the Admin Panel:
- **Dashboard**: High-level system statistics and quick actions.
- **Manage Books**: Full control over library inventory (CRUD).
- **Categories**: Create and manage book categories.
- **Operations**:
  - **All Orders**: Oversee and manage all borrowing/purchase orders.
  - **Return Requests**: Monitor return workflows.
  - **Refund History**: Auditable history of all financial refunds.
- **Management**:
  - **Manage Librarians**: Create new Librarian accounts and manage existing ones.
  - **Manage Users**: View student list, toggle active status, or delete accounts.
- **Analytics & Reports**:
  - **Reports**: General system reports.
  - **Reading Analytics**: Insights into most popular books and reading trends.
  - **Audit Logs**: Security and activity logs for compliance.
- **Contact Messages**: View and respond to inquiries from the "Contact Us" page.

### 🔔 Notification System
The system implements a persistent, database-driven notification system:
- **Real-time Updates**: Users receive alerts for order status changes (e.g., "Order Approved", "Book Returned").
- **Unread Tracking**: New notifications are highlighted.
- **Batch Actions**: "Mark all as read" functionality via AJAX for a smooth user experience.
- **Persistence**: Notifications are stored in the database so users never miss an update even if they are offline.

## 🛠 Technology Stack

| Category | Technology |
|----------|------------|
| **Backend** | ASP.NET Core 9.0 (C#) |
| **Frontend** | HTML5, JavaScript, Bootstrap 5.3.2 |
| **Database** | SQL Server, Entity Framework Core 9.0 |
| **Authentication** | Custom Session-based Auth (Role-based) |
| **Security** | Password Hashing, HttpOnly Cookies, CSRF Protection |
| **Libraries** | jQuery, Select2, Bootstrap Icons |

## 🏗 Architecture
The project follows a modular structure separating concerns between the Web presentation and Infrastructure.

### Domain Entities
The core business logic is built around the following entities:
*   **User**: Base user entity for all roles.
*   **Role**: Role definitions (Student, Librarian, Admin).
*   **Book**: Library inventory items.
*   **Category**: Book genres/types.
*   **BookCategory**: Many-to-many relationship mapping.
*   **Cart & CartItem**: Temporary storage for user selection.
*   **Order & OrderItem**: Finalized borrowing/purchase records.
*   **Payment**: Financial transaction records.
*   **Wishlist**: Saved items for future reference.
*   **Notification**: User alerts system.
*   **AuditLog**: Security tracking.
*   **ContactMessage**: User inquiries.

### Project Structure
```
BookNest/Library/
├── OnlineLibrary.Domain/        # (Infrastructure) Entities & Enums
│   ├── Entities/
│   │   ├── AuditLog.cs
│   │   ├── Book.cs
│   │   ├── BookCategory.cs
│   │   ├── Cart.cs
│   │   ├── CartItem.cs
│   │   ├── Category.cs
│   │   ├── ContactMessage.cs
│   │   ├── Notification.cs
│   │   ├── Order.cs
│   │   ├── OrderItem.cs
│   │   ├── Payment.cs
│   │   ├── Role.cs
│   │   ├── User.cs
│   │   └── Wishlist.cs
│
├── OnlineLibrary.Infrastructure/ # Data Access & Security
│   ├── Data/
│   │   ├── ApplicationDbContext.cs (EF Core Context)
│   │   └── DbInitializer.cs (Seeding)
│   ├── Security/
│   │   └── PasswordHelper.cs
│
├── OnlineLibrary.Web/            # Presentation Layer (MVC)
│   ├── Controllers/
│   │   ├── AccountController.cs (Auth)
│   │   ├── AdminController.cs
│   │   ├── LibrarianController.cs
│   │   └── StudentController.cs
│   ├── Views/ (Razor Pages)
│   │   ├── Shared/
│   │   │   ├── _AdminSidebar.cshtml
│   │   │   ├── _LibrarianSidebar.cshtml
│   │   │   └── _StudentSidebar.cshtml
│   ├── wwwroot/ (Static Assets)
│   └── appsettings.json (Config)
```

## 📋 Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server (LocalDB or Express)
- Visual Studio 2022 (v17.8+) or VS Code

## 🚀 Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/BookNest.git
   cd BookNest/Library
   ```

2. **Configure Database**
   Update the connection string in `OnlineLibrary.Web/appsettings.json` to point to your local SQL Server instance.

3. **Restore Packages**
   ```bash
   dotnet restore
   ```

4. **Apply Migrations**
   Initialize the database and apply Entity Framework migrations.
   ```bash
   cd OnlineLibrary.Web
   dotnet ef database update --project ../OnlineLibrary.Infrastucture
   ```
   *(Note: Verify the path to the Infrastructure project, it may contain a typo in the folder name `OnlineLibrary.Infrastucture`).*

5. **Run the Application**
   ```bash
   dotnet run
   ```

6. **Access the App**
   Open your browser to `https://localhost:7157` (or similar port indicated in console).

## ⚙ Configuration

### Database
Configure your SQL Server connection in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=OnlineLibraryDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

### Session Settings
Session behaviors (timeouts, cookie names) are configured in `Program.cs`. Default timeout is **30 minutes**.

## 🔑 Default Roles
- **Admin**: Full system access, User/Librarian management, Reports.
- **Librarian**: Order processing, Returns, Refunds.
- **Student**: Standard user, Borrowing, Purchasing.

---
*Generated for BookNest - Online Library System*
