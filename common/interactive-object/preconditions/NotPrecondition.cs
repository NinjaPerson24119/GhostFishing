public class NotPrecondition : IInteractiveObjectPrecondition {
    private IInteractiveObjectPrecondition _precondition;

    public NotPrecondition(IInteractiveObjectPrecondition precondition) {
        _precondition = precondition;
    }

    public bool Check(InteractiveObject interactiveObject, Player player) {
        return !_precondition.Check(interactiveObject, player);
    }
}
