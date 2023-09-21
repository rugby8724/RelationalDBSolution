
using SQLServerUI;
using DataAccessLibrary;
using DataAccessLibrary.Models;


SqlCrud sql = new SqlCrud(ConnectionString.GetConnectionString());


//DbCalls.ReadAllContacts(sql);

//DbCalls.ReadContact(sql, 1);

DbCalls.CreateNewContact(sql);
