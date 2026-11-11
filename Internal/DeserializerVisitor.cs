using PolyType.Abstractions;

namespace AssetManager.Internal
{
    public class DeserializerVisitor : TypeShapeVisitor
    {
        public override object VisitObject<T>(IObjectTypeShape<T> objectShape, object state = null)
        {
            return base.VisitObject(objectShape, state);
        }
    }
}
