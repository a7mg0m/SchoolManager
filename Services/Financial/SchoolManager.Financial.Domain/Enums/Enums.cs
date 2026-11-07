namespace SchoolManager.Financial.Domain;

public enum ExpenseType { Salary, Utilities, Maintenance, Supplies, Transportation, Equipment, Event , Other }

public enum RevenueType { Tuition, Donation, Grant, Event, Other }

public enum FeeType { Tuition, Transportation, Books, Uniform, Activity, Registration, Other }

public enum FeeStatus { Pending, PartiallyPaid, Paid, Waived, Overdue }

public enum InvoiceStatus { Draft, Issued, PartiallyPaid, Paid, Overdue, Cancelled }

public enum InvoiceType { Tuition, Registration, Transportation, Miscellaneous }

public enum PaymentStatus { Pending, Completed, Failed, Refunded }

public enum PaymentType { Cash, CreditCard, BankTransfer, Online, Cheque }
