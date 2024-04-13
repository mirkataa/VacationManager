# Vacation Manager

## Overview
This project is a Vacation Manager system designed for managing users vacation days, projects, teams, roles, and leave requests within an organization. It allows users to submit requests for paid, unpaid, and sick leave, which must be approved by their team leader or the CEO of the company.

## Technology Stack
- **.NET 8**: The project is developed using  .NET 8 and Entity Framework (Visual Studio 2022).
- **MVC (Model-View-Controller) Architecture**: Utilizes the MVC pattern for structuring the application.
- **Database**: MSSQL server
- **C#**: Primary programming language for the backend development.
- **JavaScript**: Used for frontend interactivity.
- **HTML** and **CSS**: Used for better user experience.

## Components
1. **Database**: Stores all the data related to users, roles, teams, projects, leave requests, and vacation days.
2. **Web Application**: The user interface layer that interacts with the database and provides functionality to users.

## Setup Instructions
1. Clone the repository.
2. Open the project in a .NET IDE.
3. Run the project to start the web application.
4. Access the application through a web browser.
5. **For first use**: Login with the dummyCEO to create a new CEO Account
- **Username**: dummyCEO
- **Password**: Test1234_
6. Make sure to add **Vacation Days** for **each user** for **each year (current and previous)**, otherwise, the project may not work as expected. This is because the project works with historical data (data on vacation days and remaining unused days from the previous year)

## Functionality
### Users
- Users have unique usernames and passwords.
- Each user has a first name, last name, email, role, and team.
- Users can submit and view leave requests for paid, unpaid, and sick leave.
- CRUD operations for users are available, accessible only to users with the "CEO" role.
- Users can be filtered and displayed in paginated views.
- In the users Details page, the team of the user could be changed based on his role. Again this action is accessible only to users with the "CEO" role.

### Roles
- Roles include "CEO", "Developer", "Team Lead", and "Unassigned".
- Each role has assigned users.
- CRUD operations for roles are available, accessible to users with the "CEO" role.

### Teams
- Each user can belong to a team.
- Teams are associated with projects and have developers and a team leader.
- CRUD operations for teams are available (accessible to users with the "CEO" role), with filtering by project or team name.
- In the teams Details page, developers could be added or removed. Again this action is accessible only to users with the "CEO" role.

### Projects
- Projects have names, descriptions, and associated teams.
- CRUD operations for projects are available (accessible to users with the "CEO" role), with filtering by name and description.
- In the projects Details page, teams could be added or removed. Again this action is accessible only to users with the "CEO" role.

### Leave Requests
- Users can submit leave requests specifying dates of absence, half-day leave option, and approver (the team lead of his team, or the CEO if the team lead is absented).
- Sick leave requests require an attached medical certificate.
- Leave requests can only be approved by higher-ranking employees who are not absented.
- Users can view, delete, edit, and submit leave requests.(Delete and edit only if the request has not been reviewed).
- Pagination and filtering by date are supported.
- Leave Requests which have not been reviewed by the time of the first day of absence are being automatically deleted and if the request is for paid leave, the days are returned and not marked as used. 
- If the user is on leave(approved) and create a sick leave (the period of which falls within the range of the vacation leave), then the days which have not been used from that approved vacation leave are returned to the user and not marked as used. 
- Sick Leaves are being approved immediately after their creation.
- The CEO's leave request can only be approved by another CEO
- Days of paid leave, which has not yet been approved, are stored as pending. 

## Additional Requirements
- All forms in the web application are validated to prevent empty inputs and ensure data integrity.
- Negative dates and excessively long strings are not allowed.

## Access of the different roles:
### CEO: 
- Has access to all pages and functionalities.
- ***Only he can Register new users.***
### Team Lead:
- Has access to the Teams pages (he could view only his team data)
- Has access to the Leave Requests pages (he could also review the leave requests of the members of his team and see his own requests)
### Developer and Unassigned
- Have access only to there own Leave Requests 

## Contributors
- Mirena Koleva
