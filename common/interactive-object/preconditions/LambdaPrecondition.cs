public class LambdaPrecondition : IInteractiveObjectPrecondition {
    public delegate bool Lambda(InteractiveObject interactiveObject, Player player);
    private Lambda _check;

    public LambdaPrecondition(Lambda check) {
        _check = check;
    }

    public bool Check(InteractiveObject interactiveObject, Player player) {
        return _check(interactiveObject, player);
    }
}
