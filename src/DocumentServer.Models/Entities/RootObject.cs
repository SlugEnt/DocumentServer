namespace SlugEnt.DocumentServer.Models.Entities;

/// <summary>
/// A Root Object is some key object in an external system that you want to store documents about.  It could be a Claim or a Referral or an Account.
/// It is considered the owner of a document and it alone determines how long a document is stored, when it is deleted, archived, etc.
/// </summary>
public class RootObject : AbstractBaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }


    // Relationships

    public int ApplicationId { get; set; }
    public Application Application { get; set; }
}