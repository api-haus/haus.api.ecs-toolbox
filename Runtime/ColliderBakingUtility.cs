using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using Math = Unity.Physics.Math;

namespace ECSToolbox.Runtime
{
	public class ColliderBakingUtility
	{
		/// <summary>
		/// Gets the collider bake matrix for the given collider authoring component world transform.
		/// </summary>
		/// <param name="localToWorldMatrix">World transform of the collider authoring component.</param>
		/// <param name="bodyLocalToWorldMatrix">World transform of the body the resultant baked collider will be added to.</param>
		/// <returns>Bake matrix, applied to the collider geometry during baking.</returns>
		protected static Matrix4x4 GetColliderBakeMatrix(Matrix4x4 localToWorldMatrix, Matrix4x4 bodyLocalToWorldMatrix)
		{
			var localToWorld = (float4x4)localToWorldMatrix;
			var bodyLocalToWorld = (float4x4)bodyLocalToWorldMatrix;

			// We don't bake pure uniform scales into colliders since edit-time uniform scales
			// are baked into the entity's LocalTransform.Scale property, unless the shape has non-identity scale
			// relative to its contained body. In this case we need to bake all scales into the collider geometry.
			var relativeTransform = math.mul(math.inverse(bodyLocalToWorld), localToWorld);

			var hasNonIdentityScaleRelativeToBody = relativeTransform.HasNonIdentityScale();
			var hasShearRelativeToBody = relativeTransform.HasShear();
			var bakeUniformScale = hasNonIdentityScaleRelativeToBody || hasShearRelativeToBody;

			// If the body transform has purely uniform scale, and there is any scale or shear between the body and the shape,
			// then we need to extract the uniform body scale from the shape transform before baking
			// to prevent the shape from being scaled by the body's uniform scale twice. This is because pure top level body uniform scales
			// are not baked into collider geometry but represented by the body entity's LocalTransform.Scale property.
			if (bakeUniformScale)
			{
				var bodyHasShear = bodyLocalToWorld.HasShear();
				var bodyHasNonUniformScale = bodyLocalToWorld.HasNonUniformScale();
				if (!bodyHasShear && !bodyHasNonUniformScale)
				{
					// extract uniform scale of body and remove it from the shape transform
					var bodyScale = bodyLocalToWorld.DecomposeScale();
					var bodyScaleInverse = 1 / bodyScale;
					localToWorld = math.mul(localToWorld, float4x4.Scale(bodyScaleInverse));
				}
			}

			// only bake shear or non-uniform scales into the collider geometry
			if (bakeUniformScale || localToWorld.HasShear() || localToWorld.HasNonUniformScale())
			{
				var rigidBodyTransform = Math.DecomposeRigidBodyTransform(localToWorld);
				var bakeMatrix = math.mul(math.inverse(new float4x4(rigidBodyTransform)), localToWorld);
				// make sure we have a valid transformation matrix
				bakeMatrix.c0[3] = 0;
				bakeMatrix.c1[3] = 0;
				bakeMatrix.c2[3] = 0;
				bakeMatrix.c3[3] = 1;
				return bakeMatrix;
			}
			// else:

			return float4x4.identity;
		}

		public static CapsuleGeometry CapsuleGeometry(UnityEngine.CapsuleCollider shape, Transform bodyTransform)
		{
			var bakeToShape = GetColliderBakeMatrix(shape.transform.localToWorldMatrix, bodyTransform.localToWorldMatrix);
			var lossyScale = math.abs(bakeToShape.lossyScale);

			// the capsule axis corresponds to the local axis specified by the direction index.
			var capsuleAxis = new float3 { [shape.direction] = 1f };

			// the baked radius is the user-specified shape radius times the maximum of the scale of the two axes orthogonal to the capsule axis.
			var radius = shape.radius * math.cmax(new float3(lossyScale) { [shape.direction] = 0f });

			// the capsule vertex offset points from the center of the capsule to the top of the capsule's cylindrical center part.
			var vertexOffset = capsuleAxis * (0.5f * shape.height * lossyScale[shape.direction] - radius);

			// finish baking the vertex offset by rotating it with the bake matrix
			vertexOffset = math.rotate(bakeToShape.rotation, vertexOffset);

			// bake the capsule's center
			var center = math.transform(bakeToShape, shape.center);

			// the capsule's two baked vertices are the baked center plus/minus the baked vertex offset
			var v0 = center + vertexOffset;
			var v1 = center - vertexOffset;

			return new CapsuleGeometry { Vertex0 = v0, Vertex1 = v1, Radius = radius };
		}
	}
}