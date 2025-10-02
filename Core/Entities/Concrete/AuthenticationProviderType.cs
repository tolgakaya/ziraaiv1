namespace Core.Entities.Concrete
{
    public enum AuthenticationProviderType
    {
        /// <summary>
        /// It should not be. Corresponds to the 0 code. It has been added to detect errors.
        /// </summary>
        Unknown,

        /// <summary>
        /// Login for Person
        /// </summary>
        Person,

        /// <summary>
        /// Login for Institution staff
        /// </summary>
        Staff,

        /// <summary>
        /// Login for contract service or outsource personnel.
        /// </summary>
        Agent,

        /// <summary>
        /// Login with phone number and OTP (SMS-based authentication)
        /// </summary>
        Phone
    }
}