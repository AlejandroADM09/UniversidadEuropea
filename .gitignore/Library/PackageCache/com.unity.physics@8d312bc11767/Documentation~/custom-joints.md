# Joints in Unity Physics

Learn about the `PhysicsJoint` data component and the joint types Unity Physics provides to link entities together.

The [`PhysicsJoint`](xref:Unity.Physics.PhysicsJoint) data component differs from the other components described in this section. It links two entities together with a constraint, such as a door hinge. These entities need `PhysicsCollider` components, and at least one of them needs a `PhysicsVelocity` component. Otherwise the joint has no effect. During the physics step, Unity Physics solves the joint and the contacts that affect each entity.

The `JointData` property describes the joint's behavior. Like the `PhysicsCollider` component, this is a `BlobAssetReference` to a `JointData`. The precise behavior of each joint depends on the type of this data.

Unity Physics provides several pre-created joint types. Each joint type has a static creation function in `Unity.Physics.JointData`, and, as with shapes, the input parameters vary between types. For example, the `CreateBallAndSocket` method needs to know the joint's location relative to each body. The `CreateLimitedHinge()` method also needs to know which axis the bodies can rotate about, and the minimum and maximum limit for this rotation.

Unity Physics provides the following joint types:

| **Joint** | **Description** |
|---|---|
| Ball and Socket | Allows motion around an indefinite number of axes. Humans have such joints in the hips and shoulders. |
| Limited Hinge | Allows limited articulation on one axis. Humans have such joints in the fingers and knees. |
| Fixed | Constrains two rigid bodies together, removing their ability to act independently of each other. |
| Hinge | Allows free rotation on one axis. Use for spinning wheels and carousels. |
| Prismatic | Constrains two bodies to a sliding motion on one axis. Use for sliding doors. |
| Ragdoll | Limits the motion on a few axes. Useful for character ragdolls. |
| Stiff Spring | Constrains two bodies to be a certain distance apart from each other. |

Alongside the joint data and the entities, an important setting is `EnableCollision`. This defaults to off, which is the recommended setting. If you have two bodies constrained together (such as a door attached to a car), they probably overlap each other to some degree. In this case, the joint pulls the objects together while collision detection pushes them apart. This causes unstable simulation or raises too many events. When `EnableCollision` is off, the physics simulation doesn't perform collision detection between the two bodies, even if the collider's collision filter otherwise allows them to collide.

> **Note**: If multiple joints exist between a pair of bodies, Unity Physics enables collisions when any of the joints requests collision.

Optionally, add a [`PhysicsSolverType`](xref:Unity.Physics.PhysicsSolverType) component to a joint entity to choose the solver that simulates the joint's dynamics. Without it, Unity Physics uses the default **Iterative Solver**, which is fast and approximate. With it, you can select the **Direct Solver** instead. The direct solver yields accurate results but is more computationally demanding. It's ideal for situations that involve complex jointed mechanisms with many joints, and high mass or stiffness ratios. The direct solver lets you create more advanced physics-based game elements. Refer to the [Constraint solvers](constraint-solvers.md) section for more details.

## Additional resources

- [Constraint solvers](constraint-solvers.md)
- [Rigid bodies as entities and data components](concepts-data.md)
- [The simulation pipeline](concepts-simulation.md)
