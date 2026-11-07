using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks
{
    public sealed class Money
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money() : this(0, "EGP") { }
        public Money(decimal amount, string currency = "EGP")
        {
            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative");

            Amount = amount;
            Currency = currency;
        }
        public static Money Zero(string currency = "EGP")
      => new Money(0, currency);
        
        public Money Add(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException("Currency mismatch");

            return new Money(Amount + other.Amount, Currency);
        }
        public Money Subtract(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException("Currency mismatch");

            return new Money(Amount - other.Amount, Currency);
        }
        public Money Multiply(decimal factor)
        {
            return new Money(Amount * factor, Currency);
        }
    }
}
