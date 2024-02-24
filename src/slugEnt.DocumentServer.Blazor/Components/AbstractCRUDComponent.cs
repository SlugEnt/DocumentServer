using Microsoft.AspNetCore.Components;
using Radzen;

namespace SlugEnt.DocumentServer.Blazor.Components;

public abstract class AbstractCRUDComponent : ComponentBase
{
    [Parameter] public string mode { get; set; }
    [Parameter] public int? recordId { get; set; }

    [Inject] protected NavigationManager _navigationManager { get; set; }

    protected string  _entityName     = "";
    protected Variant radDisplay      = Variant.Outlined;
    protected string  _pageTitle      = "";
    protected bool    _isInitializeed = false;
    protected bool    _isViewOnly     = false;
    protected bool    _isCreateMode   = false;
    protected bool    _isEditMode     = false;
    protected bool    _isDeleteMode   = false;
    protected bool    _wasDeleted     = false;
    protected bool    _isReadOnly     = false;
    protected bool    _canEdit        = false; // Determines if fields can be edited.
    protected bool    _isDisabled     = false; // These are fields that can only be edited during creation.  They are not editable in edit mode.
    protected bool    _errVisible     = false;
    protected string  _errMsg         = "";
    protected string  _successMsg     = "";


    public AbstractCRUDComponent(string entityName) { _entityName = entityName; }
    public AbstractCRUDComponent() { }


    public override async Task SetParametersAsync(ParameterView parameters) { await base.SetParametersAsync(parameters); }


    /// <summary>
    /// Explanation of the Modes:
    ///   Create sets Createmode and sets readonly to false so fields can be edited.
    ///   Edit sets EditMode and sets readonlyto false (so fields can be edited), but IsDisabled to true so that fields that cannot be edited once saved are disabled.
    ///   Delete sets IsDeleteMode and sets Readonly to true, so no fields can be edited.
    ///   View sets IsViewOnlt and ReadOnly to true.
    /// </summary>
    /// <returns></returns>
    protected override async Task OnParametersSetAsync()
    {
        // Set the page mode.
        if (mode == "C")
        {
            _isCreateMode = true;
            _isReadOnly   = false;
            _pageTitle    = "Create " + _entityName;
        }


        else if (mode == "E")
        {
            _isEditMode = true;
            _isReadOnly = false;
            _isDisabled = true;
            _pageTitle  = "Edit " + _entityName;
        }

        else if (mode == "D")
        {
            _isDeleteMode = true;
            _isReadOnly   = true;
            _pageTitle    = "Confirm Deletion of " + _entityName;
        }

        else
        {
            _isViewOnly = true;
            _isReadOnly = true;
            _pageTitle  = "View Root " + _entityName;
        }


        if (recordId == null && !_isCreateMode)
        {
            _errVisible = true;
            _errMsg     = "No value provided for the Id to retrieve.";
        }

        // Call derived class and tell it Base Parameters have been set.
        await PostSetParametersAsync();
    }



    /// <summary>
    /// Cancels and returns to the Index
    /// </summary>
    public void Cancel() { ReturnToList(); }



    /// <summary>
    /// Returns to the objects Index page
    /// </summary>
    protected void ReturnToList() { _navigationManager.NavigateTo("/rootobjects"); }



    /// <summary>
    /// Clears Error and Success Messages
    /// </summary>
    protected void ClearMessages()
    {
        _errMsg     = "";
        _successMsg = "";
    }


    /// <summary>
    /// Derived Objects should set this.  This is where you would read in the record(s) you need.
    /// </summary>
    /// <returns></returns>
    protected virtual Task PostSetParametersAsync() { return Task.CompletedTask; }
}