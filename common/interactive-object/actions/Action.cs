using System.Collections.Generic;

public abstract class InteractiveObjectAction {
    public string Description {
        get => _description;
    }
    private string _description;

    protected List<IInteractiveObjectPrecondition> _preconditions = new List<IInteractiveObjectPrecondition>();

    public InteractiveObjectAction(string description) {
        _description = description;
    }

    public bool CheckPreconditions(InteractiveObject interactiveObject, Player player) {
        for (int i = 0; i < _preconditions.Count; i++) {
            if (!_preconditions[i].Check(interactiveObject, player)) {
                return false;
            }
        }
        return true;
    }

    public virtual bool Activate(InteractiveObject interactiveObject, Player player) {
        return CheckPreconditions(interactiveObject, player);
    }
}
