# Spec Delta

## Purpose

Lets administrators read, in the backoffice, every lead the public website has collected, with its email and when it
was registered.

## ADDED Requirements

### Requirement: Signed-in staff reach the leads screen

The backoffice SHALL offer a signed-in screen at `/customer-leads` with the heading "Leads", shown with the navigation
drawer and header like every signed-in screen. A visitor without a session SHALL end on `/login`.

#### Scenario: Admin opens the screen

- **WHEN** an admin with a stored session opens `/customer-leads`
- **THEN** the heading "Leads" is shown with the drawer and header

#### Scenario: Visitor without a session

- **WHEN** a visitor with no stored session opens `/customer-leads`
- **THEN** the visitor ends on `/login`

#### Scenario: Heading in English

- **WHEN** an admin opens the screen in English
- **THEN** the heading reads "Leads"

### Requirement: The screen lists every lead the API returns

The screen SHALL show one row per lead the API returns, in the order the API returns them. It SHALL NOT sort, filter or
hide any lead.

#### Scenario: Leads shown in API order

- **WHEN** an admin opens the screen and the API returns a lead registered today and another registered yesterday,
  in that order
- **THEN** the table shows two rows, today's first and yesterday's second

### Requirement: Each row shows the email and the registration date

Each row SHALL show the lead's email under the column "Email" and its registration time under the column "Fecha de
registro". The registration time SHALL be shown in the user's language and local time zone, with the date and the time
in hours and minutes. In English the columns SHALL read "Email" and "Registered on".

#### Scenario: Row content

- **WHEN** the API returns the lead "ana@example.com" registered on "2026-11-10T08:15:00Z"
- **THEN** its row shows "ana@example.com"
- **AND** the date and time of "2026-11-10T08:15:00Z" in the user's language and local time zone

#### Scenario: Columns in English

- **WHEN** the screen is shown in English
- **THEN** the columns read "Email" and "Registered on"

### Requirement: The screen shows loading, empty and failure states

While the leads load, the screen SHALL show a loading placeholder instead of the table. An empty list SHALL show
"Todavía no hay leads." ("No leads yet." in English). A failed load SHALL show the error for its code with a retry
action, and no table.

#### Scenario: Loading

- **WHEN** the leads have not loaded yet
- **THEN** a loading placeholder is shown and no table is shown

#### Scenario: Empty list

- **WHEN** the API returns no leads
- **THEN** "Todavía no hay leads." is shown instead of rows

#### Scenario: Load fails and is retried

- **WHEN** the listing fails
- **THEN** the error for its code is shown with a retry action
- **AND** activating retry requests the leads again

#### Scenario: Trainer opens the URL directly

- **WHEN** a trainer opens `/customer-leads` and the listing fails with `FORBIDDEN`
- **THEN** the error for `FORBIDDEN` is shown with a retry action
- **AND** no table and no lead is shown
