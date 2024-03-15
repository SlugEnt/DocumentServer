using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SlugEnt.DocumentServer.Models.Entities;

/// <summary>
///     An Application that needs to store documents
/// </summary>
public class Application : AbstractBaseEntity
{
    // Relationships

    // Each App has 1 or more Document Types it manages.
    public ICollection<DocumentType>? DocumentTypes;
    public ICollection<RootObject>?   RootObjects;


    /// <summary>
    ///     For displaying information about this in an error type message
    /// </summary>
    [NotMapped]
    public string ErrorMessage
    {
        get
        {
            string className = GetType().Name;
            string msg = string.Format("{0}:  [Id: {1} | Name: {2} ]",
                                       className,
                                       Id,
                                       Name);
            return msg;
        }
    }

    /// <summary>
    ///     Id
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    ///     The name of the Application.  This can be full English Description
    /// </summary>
    [MaxLength(75)]
    public string Name
    {
        get { return _name; }
        set
        {
            if (value == null)
                _name = string.Empty;
            else
            {
                _name = value.Trim();
                if (_name.Length > 75)
                    _name = _name.Substring(0, 75);
            }
        }
    }

    private string _name;

    /// <summary>
    /// The token required to read / update any documents in the App library
    /// </summary>
    [MaxLength(32)]
    public string Token { get; set; }
}