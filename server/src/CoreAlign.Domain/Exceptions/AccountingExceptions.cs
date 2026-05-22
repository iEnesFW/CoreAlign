namespace CoreAlign.Domain.Exceptions;

public class GLAccountNotFoundException : NotFoundException
{
    public GLAccountNotFoundException(Guid id) : base($"GL account {id} not found.") { }
    public GLAccountNotFoundException(string code) : base($"GL account with code '{code}' not found.") { }
}

public class GLAccountCodeConflictException : ConflictException
{
    public GLAccountCodeConflictException(string code) : base($"GL account with code '{code}' already exists.") { }
}

public class GLAccountHasChildrenException : ConflictException
{
    public GLAccountHasChildrenException()
        : base("Cannot delete an account that has children. Delete the children first.") { }
}

public class GLAccountPostableInvariantException : ConflictException
{
    public GLAccountPostableInvariantException()
        : base("Parent accounts cannot be marked postable. Posting happens at leaves only.") { }
}

public class GLAccountInvalidTypeException : DomainException
{
    public GLAccountInvalidTypeException(string raw) : base($"Invalid AccountType '{raw}'.") { }
}

public class JournalEntryNotFoundException : NotFoundException
{
    public JournalEntryNotFoundException(Guid id) : base($"Journal entry {id} not found.") { }
}

public class JournalEntryNotBalancedException : DomainException
{
    public JournalEntryNotBalancedException(decimal totalDebit, decimal totalCredit)
        : base($"Journal entry is unbalanced: debit {totalDebit} ≠ credit {totalCredit}.") { }
}

public class JournalEntryEmptyException : DomainException
{
    public JournalEntryEmptyException() : base("Journal entry must have at least two lines before posting.") { }
}

public class JournalLineNotPostableException : ConflictException
{
    public JournalLineNotPostableException(string accountCode)
        : base($"Account '{accountCode}' is not postable (posting only happens at leaf accounts).") { }
}

public class JournalLineInactiveAccountException : ConflictException
{
    public JournalLineInactiveAccountException(string accountCode)
        : base($"Account '{accountCode}' is inactive and cannot receive new postings.") { }
}

public class JournalEntryStatusTransitionException : DomainException
{
    public JournalEntryStatusTransitionException(string status, string action)
        : base($"Cannot {action} journal entry in status '{status}'.") { }
}

public class JournalLineSidesException : DomainException
{
    public JournalLineSidesException()
        : base("A journal line must have either Debit or Credit > 0, not both and not neither.") { }
}

public class JournalEntryInvalidTypeException : DomainException
{
    public JournalEntryInvalidTypeException(string raw) : base($"Invalid JournalEntryType '{raw}'.") { }
}
