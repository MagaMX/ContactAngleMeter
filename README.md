# Сontact Angle Meter
This is a WPF application that measures and records the contact angle of a liquid on a material surface. The program allows you to select a liquid, a material, and input a measured contact angle. The data is saved in a PostgreSQL database, and the interface supports viewing, adding, and deleting records of contact angles.

## Features

- Select a liquid and material combination from dropdown menus.
- Input the measured contact angle for the selected combination.
- Display all recorded contact angles in a `DataGrid`.
- Add new measurements to the database.
- Delete selected records from the database via the `DataGrid`.
- Supports PostgreSQL for persistent storage of data.

## Technologies Used

- **C# (.NET)**: For application development with WPF.
- **WPF (Windows Presentation Foundation)**: For building the user interface.
- **PostgreSQL**: As the relational database to store liquid, material, and angle data.
- **Npgsql**: .NET data provider for PostgreSQL.
  
## Prerequisites

Before running the program, ensure you have the following installed:

- .NET 6 SDK or higher
- PostgreSQL (with an accessible instance)
- Npgsql library (this can be added via NuGet in Visual Studio)

## Examples (screenshots)

Uploading picture:  ![Капля на Al](https://github.com/user-attachments/assets/8acf923d-295a-4d52-a752-00228e4922ab)

Getting result:   ![image](https://github.com/user-attachments/assets/8ca4e1b2-6284-4d3e-964d-fa0a64936f9a)



