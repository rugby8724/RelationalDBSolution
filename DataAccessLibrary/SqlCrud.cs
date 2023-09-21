using DataAccessLibrary.Models;
using System.Xml.Serialization;

namespace DataAccessLibrary
{
    public class SqlCrud
    {
        private readonly string connectionString;
        private SqlDataAccess db = new SqlDataAccess();
        public SqlCrud(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public List<BasicContactModel> GetAllContacts()
        {
            string sql = "select Id, FirstName, LastName from dbo.Contacts";

            return db.LoadData<BasicContactModel, dynamic>(sql, new { }, connectionString);

        }

        public FullContactModel GetFullContactById (int id)
        {
            string sql = "select Id, FirstName, LastName from dbo.Contacts where Id = @Id";

            FullContactModel output = new FullContactModel();

            output.BasicInfo = db.LoadData<BasicContactModel, dynamic>(sql, new { Id = id }, connectionString).FirstOrDefault();

            if (output.BasicInfo == null)
            {
                // do something to tell the user that the record was not found
                // throw new Exception("User not found");
                return null;
            }

            sql = @"select e.* 
                   from dbo.EmailAddresses e
                   inner join dbo.ContactEmail ce on ce.EmailAddressId = e.Id
                   where ce.ContactId = @Id";


            output.EmailAddresses = db.LoadData<EmailAddressModel, dynamic>(sql, new { id }, connectionString);

            sql = @"select p.* 
                   from dbo.PhoneNumbers p
                   inner join dbo.ContactPhoneNumbers cp on cp.PhoneNumberId = p.Id
                   where cp.ContactId = @Id";


            output.PhoneNumbers = db.LoadData<PhoneNumberModel, dynamic>(sql, new { id }, connectionString);


            return output;


        }

        public void CreateContact(FullContactModel contact)
        {
            // Save the basic contact
            string sql = "insert into dbo.Contacts (FirstName, LastName) values (@FirstName, @LastName);";
            db.SaveData(sql,
                        new { contact.BasicInfo.FirstName, contact.BasicInfo.LastName },
                        connectionString);

            // Get the ID number of the contact
            sql = "select Id from dbo.Contacts where FirstName = @FirstName and LastName = @LastName;";
            var contactId = db.LoadData<IdLookupModel, dynamic>(sql,
                                                         new { contact.BasicInfo.FirstName, contact.BasicInfo.LastName },
                                                         connectionString).First().Id;

            // Identify if the Phone number exist
            foreach (var phoneNumber in contact.PhoneNumbers)
            {
                if (phoneNumber.Id == 0)
                {
                    sql = "insert into dbo.PhoneNumbers (PhoneNumber) values (@PhoneNumber);";
                    db.SaveData(sql, new { phoneNumber.PhoneNumber }, connectionString);

                    sql = "select Id from dbo.PhoneNumbers where PhoneNumber = (@PhoneNumber);";
                    phoneNumber.Id = db.LoadData<IdLookupModel, dynamic>(sql, new { phoneNumber.PhoneNumber }, connectionString).First().Id;
                }

                sql = "insert into dbo.ContactPhoneNumbers (ContactId, PhoneNumberId) values (@ContactId, @PhoneNumberId);";
                db.SaveData(sql, new { ContactId = contactId, PhoneNumberId = phoneNumber.Id }, connectionString);
            }
            // Identify if the Email Address exist

            foreach (var email in contact.EmailAddresses)
            {
                if (email.Id == 0)
                {
                    sql = "insert into dbo.EmailAddresses (EmailAddress) values (@EmailAddress);";
                    db.SaveData(sql, new { email.EmailAddress }, connectionString);

                    sql = "select Id from dbo.EmailAddresses where EmailAddress = (@EmailAddress);";
                    email.Id = db.LoadData<IdLookupModel, dynamic>(sql, new { email.EmailAddress }, connectionString).First().Id;
                }

                sql = "insert into dbo.ContactPhoneNumbers (ContactId, EmailAddressId) values (@ContactId, @EmailAddressId);";
                db.SaveData(sql, new { ContactId = contactId, EmailAddressId = email.Id }, connectionString);










            }
        }

        public void UpDateContactName(BasicContactModel contact)
        {
            string sql = "update dbo.Contacts set FirstName = @FirstName, LastName = @LastName where Id = @Id";
            db.SaveData(sql, contact, connectionString);
        }

        public void RemovePhoneNumberFromContact(int contactId, int phoneNumberId)
        {
            // Find all of the usages of the phone number id
            // If 1, then delete link and phone number
            // If >, then delete link for contact
            string sql = "select Id, ContactId, PhoneNumberId from dbo.ContactPhoneNumbers where PhoneNumberId = @PhoneNumberId";

            var links = db.LoadData<ContactPhoneNumberModel, dynamic>(sql,
                                                                      new { PhoneNumberId = phoneNumberId },
                                                                      connectionString);

            sql = "delete from dbo.ContactPhoneNumbers where PhoneNumberId = @PhoneNumberId and ContactId = @ContactId";

            db.SaveData(sql, new { PhoneNumberID = phoneNumberId, ContactId = contactId }, connectionString);
            if (links.Count == 1)
            {
                sql = "delete from dbo.PhoneNumbers where Id = @PhoneNumberID;";
                db.SaveData(sql, new { PhoneNumberId = phoneNumberId }, connectionString);
            }
        }
    }
}
