# Description
As a business user, I want to see all customers that have at least one overdue order, so that I can identify accounts that require follow up.

An order is considered overdue when its due date is earlier than today and its status is not closed.

# Acceptance Criteria
- The feature lists only customers that have at least one overdue order.
- Orders are grouped by customer.
- Customers are ordered by the date of their oldest overdue order, ascending.
- For each customer, the following information is displayed:
  - Customer name
  - Number of overdue orders
  - Date of the oldest overdue order
- The feature is accessible through a console command.