using System.Collections.Generic;

public abstract class InteractiveObjectAction {
    protected List<IInteractiveObjectPrecondition> _preconditions = new List<IInteractiveObjectPrecondition>();

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

    public void AddPrecondition(IInteractiveObjectPrecondition precondition) {
        _preconditions.Add(precondition);
    }
}
