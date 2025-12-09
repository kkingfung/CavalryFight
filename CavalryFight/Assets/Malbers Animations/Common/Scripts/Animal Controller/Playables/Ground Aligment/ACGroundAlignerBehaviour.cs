using UnityEngine;
using UnityEngine.Playables;

namespace MalbersAnimations.Controller
{
    public class ACGroundAlignerBehaviour : PlayableBehaviour
    {
        public float distance;
        public bool HasHipPivot;
        public MAnimal EndLocation;

        public float Offset;

        public void GroundRayCast(MAnimal animal)
        {
            var hit_Chest = new RaycastHit() { normal = Vector3.zero };             //Clean the Ray casts every time 
            var hit_Hip = new RaycastHit();                                         //Clean the Raycast every time 
            hit_Chest.distance = hit_Hip.distance = animal.Height;                 //Reset the Distances to the Height of the animal

            bool FrontRay = false; //Flag to know if the Raycast from the Chest Pivot hit the ground

            Vector3 SlopeNormal;
            Vector3 SlopeDirection;

            distance *= animal.ScaleFactor; //Scale the distance by the animal scale factor

            Vector3 MainPoint = animal.Main_Pivot_Point; //Get the Main Pivot Point (Chest or Hip) of the Animal

            animal.RB.isKinematic = false;

            MDebug.DrawWireSphere(animal.t.position, Color.black, 0.05f * animal.ScaleFactor, 1); //Draw a Sphere at the Animal Position

            if (Physics.Raycast(MainPoint, -animal.Up, out hit_Chest, distance, animal.GroundLayer, QueryTriggerInteraction.Ignore))
            {
                FrontRay = true;

                SlopeNormal = hit_Chest.normal;
                SlopeDirection = Vector3.ProjectOnPlane(animal.Gravity, SlopeNormal).normalized;

                MDebug.DrawRay(MainPoint, -animal.Up * distance, Color.blue, 0.2f);
                MDebug.DrawRay(MainPoint - animal.Up * hit_Chest.distance, 0.2f * animal.ScaleFactor * SlopeDirection, Color.red, 0.2f);
                MDebug.DrawWireSphere(MainPoint - animal.Up * hit_Chest.distance, Color.green, 0.1f * animal.ScaleFactor);
                MDebug.Draw_Arrow(hit_Chest.point, SlopeDirection * 0.5f, Color.black, 0, 0.1f);
            }


            bool MainRay;

            if (animal.Has_Pivot_Hip)
            {
                var hipPoint = animal.Pivot_Hip.World(animal.t); //Get the Hip Pivot Position

                if (Physics.Raycast(hipPoint, -animal.Up, out hit_Hip, distance, animal.GroundLayer, QueryTriggerInteraction.Ignore))
                {
                    MainRay = true;
                    if (!FrontRay) hit_Chest = hit_Hip;
                }
                else
                {
                    MainRay = false;

                    if (FrontRay) hit_Hip = hit_Chest;  //In case there's no Hip Ray
                }
            }
            else
            {
                MainRay = FrontRay; //Just in case you dont have HIP RAY IMPORTANT FOR HUMANOID CHARACTERS
                hit_Hip = hit_Chest;  //In case there's no Hip Ray
            }

            Vector3 direction = (hit_Chest.point - hit_Hip.point).normalized;
            Vector3 Side = Vector3.Cross(animal.UpVector, direction).normalized;
            Vector3 SurfaceNormal = Vector3.Cross(direction, Side).normalized;

            if (!MainRay && FrontRay)
            {
                SurfaceNormal = hit_Chest.normal;
            }

            var rot = AlignRotation(animal, SurfaceNormal);
            var pos = AlignPosition(animal, hit_Hip.distance);

            animal.t.SetPositionAndRotation(pos, rot); //Set the Animal Position and Rotation
        }

        public virtual Quaternion AlignRotation(MAnimal animal, Vector3 alignNormal)
        {
            Quaternion AlignRot = Quaternion.FromToRotation(animal.Up, alignNormal) * animal.Rotation;  //Calculate the orientation to Terrain 
            return AlignRot;
        }

        internal Vector3 AlignPosition(MAnimal animal, float distance)
        {
            float difference = (animal.Height + Offset) - distance;

            Vector3 align = animal.Rotation * new Vector3(0, difference, 0); ; //Rotates with the Transform to better alignment
            return animal.Position + align; //WORKS WITH THIS!! 
        }


    }
}
