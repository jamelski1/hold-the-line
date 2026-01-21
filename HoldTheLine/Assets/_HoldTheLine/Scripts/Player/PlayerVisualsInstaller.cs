// PlayerVisualsInstaller.cs - Replaces capsule with procedural robot visual
// Location: Assets/_HoldTheLine/Scripts/Player/
// Automatically installs robot visuals on the Player object at runtime

using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Installs a procedural robot visual on the Player object, replacing the capsule.
    /// Creates a simple humanoid robot from primitives with procedural animation.
    /// </summary>
    public class PlayerVisualsInstaller : MonoBehaviour
    {
        [Header("Robot Colors")]
        [SerializeField] private Color bodyColor = new Color(0.2f, 0.7f, 0.9f); // Cyan-ish
        [SerializeField] private Color headColor = new Color(0.3f, 0.8f, 1f);   // Lighter cyan
        [SerializeField] private Color limbColor = new Color(0.15f, 0.5f, 0.7f); // Darker cyan
        [SerializeField] private Color eyeColor = Color.white;
        [SerializeField] private Color visorColor = new Color(0.1f, 0.1f, 0.3f);

        [Header("Scale")]
        [SerializeField] private float robotScale = 1f;

        // Robot parts for animation
        private Transform visualsRoot;
        private Transform body;
        private Transform head;
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform visor;

        // Animation state
        private PlayerController playerController;
        private float animationTime;
        private float currentSpeed;
        private float targetSpeed;

        // Animation parameters
        private const float IdleBobAmount = 0.02f;
        private const float IdleBobSpeed = 2f;
        private const float WalkBobAmount = 0.05f;
        private const float WalkSpeed = 8f;
        private const float ArmSwingAmount = 25f;
        private const float LegSwingAmount = 20f;
        private const float LeanAmount = 5f;
        private const float SpeedSmoothing = 10f;

        private void Start()
        {
            playerController = GetComponent<PlayerController>();
            InstallRobotVisuals();
        }

        private void Update()
        {
            UpdateAnimation();
        }

        /// <summary>
        /// Creates the robot visual hierarchy under the Player object
        /// </summary>
        private void InstallRobotVisuals()
        {
            // Disable the original capsule mesh renderer (keep collider)
            MeshRenderer originalRenderer = GetComponent<MeshRenderer>();
            if (originalRenderer != null)
            {
                originalRenderer.enabled = false;
            }

            // Also check for MeshFilter
            MeshFilter originalMeshFilter = GetComponent<MeshFilter>();
            if (originalMeshFilter != null)
            {
                // Don't destroy - just disable renderer
            }

            // Create visuals root
            GameObject visualsObj = new GameObject("Visuals");
            visualsObj.transform.SetParent(transform);
            visualsObj.transform.localPosition = Vector3.zero;
            visualsObj.transform.localRotation = Quaternion.identity;
            visualsObj.transform.localScale = Vector3.one * robotScale;
            visualsRoot = visualsObj.transform;

            // Create robot parts
            CreateRobotBody();

            Debug.Log("[PlayerVisualsInstaller] Robot visuals installed successfully!");
        }

        private void CreateRobotBody()
        {
            // Body (torso) - main cube
            body = CreatePart("Body", PrimitiveType.Cube, bodyColor,
                new Vector3(0, 0.1f, 0),
                new Vector3(0.5f, 0.6f, 0.3f));

            // Head - cube with rounded appearance
            head = CreatePart("Head", PrimitiveType.Cube, headColor,
                new Vector3(0, 0.55f, 0),
                new Vector3(0.35f, 0.35f, 0.3f));

            // Visor/Face plate
            visor = CreatePart("Visor", PrimitiveType.Cube, visorColor,
                new Vector3(0, 0.55f, 0.12f),
                new Vector3(0.25f, 0.15f, 0.1f));

            // Eyes (two small spheres)
            CreatePart("LeftEye", PrimitiveType.Sphere, eyeColor,
                new Vector3(-0.08f, 0.58f, 0.15f),
                new Vector3(0.06f, 0.06f, 0.06f));
            CreatePart("RightEye", PrimitiveType.Sphere, eyeColor,
                new Vector3(0.08f, 0.58f, 0.15f),
                new Vector3(0.06f, 0.06f, 0.06f));

            // Left Arm
            GameObject leftArmPivot = new GameObject("LeftArmPivot");
            leftArmPivot.transform.SetParent(visualsRoot);
            leftArmPivot.transform.localPosition = new Vector3(-0.32f, 0.25f, 0);
            leftArm = leftArmPivot.transform;
            CreatePartUnder(leftArmPivot.transform, "LeftArm", PrimitiveType.Cube, limbColor,
                new Vector3(0, -0.2f, 0),
                new Vector3(0.12f, 0.4f, 0.12f));

            // Right Arm
            GameObject rightArmPivot = new GameObject("RightArmPivot");
            rightArmPivot.transform.SetParent(visualsRoot);
            rightArmPivot.transform.localPosition = new Vector3(0.32f, 0.25f, 0);
            rightArm = rightArmPivot.transform;
            CreatePartUnder(rightArmPivot.transform, "RightArm", PrimitiveType.Cube, limbColor,
                new Vector3(0, -0.2f, 0),
                new Vector3(0.12f, 0.4f, 0.12f));

            // Left Leg
            GameObject leftLegPivot = new GameObject("LeftLegPivot");
            leftLegPivot.transform.SetParent(visualsRoot);
            leftLegPivot.transform.localPosition = new Vector3(-0.12f, -0.2f, 0);
            leftLeg = leftLegPivot.transform;
            CreatePartUnder(leftLegPivot.transform, "LeftLeg", PrimitiveType.Cube, limbColor,
                new Vector3(0, -0.25f, 0),
                new Vector3(0.15f, 0.5f, 0.15f));

            // Right Leg
            GameObject rightLegPivot = new GameObject("RightLegPivot");
            rightLegPivot.transform.SetParent(visualsRoot);
            rightLegPivot.transform.localPosition = new Vector3(0.12f, -0.2f, 0);
            rightLeg = rightLegPivot.transform;
            CreatePartUnder(rightLegPivot.transform, "RightLeg", PrimitiveType.Cube, limbColor,
                new Vector3(0, -0.25f, 0),
                new Vector3(0.15f, 0.5f, 0.15f));

            // Antenna on head
            CreatePart("Antenna", PrimitiveType.Cylinder, limbColor,
                new Vector3(0, 0.8f, 0),
                new Vector3(0.03f, 0.1f, 0.03f));
            CreatePart("AntennaTop", PrimitiveType.Sphere, eyeColor,
                new Vector3(0, 0.92f, 0),
                new Vector3(0.06f, 0.06f, 0.06f));

            // Shoulder pads
            CreatePart("LeftShoulder", PrimitiveType.Sphere, bodyColor,
                new Vector3(-0.28f, 0.3f, 0),
                new Vector3(0.15f, 0.12f, 0.12f));
            CreatePart("RightShoulder", PrimitiveType.Sphere, bodyColor,
                new Vector3(0.28f, 0.3f, 0),
                new Vector3(0.15f, 0.12f, 0.12f));

            // Chest detail (badge/light)
            CreatePart("ChestLight", PrimitiveType.Sphere, eyeColor,
                new Vector3(0, 0.15f, 0.16f),
                new Vector3(0.08f, 0.08f, 0.04f));
        }

        private Transform CreatePart(string name, PrimitiveType type, Color color, Vector3 localPos, Vector3 scale)
        {
            return CreatePartUnder(visualsRoot, name, type, color, localPos, scale);
        }

        private Transform CreatePartUnder(Transform parent, string name, PrimitiveType type, Color color, Vector3 localPos, Vector3 scale)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent);
            part.transform.localPosition = localPos;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = scale;

            // Remove collider (we use the parent's collider)
            Collider col = part.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Set URP material
            Renderer rend = part.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = CreateURPMaterial(color);
            }

            return part.transform;
        }

        private Material CreateURPMaterial(Color color)
        {
            // Try URP shaders
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material mat = new Material(shader);

            // Set color based on shader type
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            else if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }

            // Set smoothness for Lit shader
            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", 0.5f);
            }

            return mat;
        }

        private void UpdateAnimation()
        {
            if (visualsRoot == null) return;

            // Get movement speed from PlayerController
            float inputSpeed = 0f;
            if (playerController != null)
            {
                // Estimate speed from position change or input
                float horizontal = 0f;
                if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) horizontal = -1f;
                else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) horizontal = 1f;
                inputSpeed = Mathf.Abs(horizontal);
            }

            // Smooth speed transition
            targetSpeed = inputSpeed;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, SpeedSmoothing * Time.deltaTime);

            // Update animation time
            animationTime += Time.deltaTime;

            // Apply animation based on speed
            if (currentSpeed > 0.1f)
            {
                AnimateWalking();
            }
            else
            {
                AnimateIdle();
            }
        }

        private void AnimateIdle()
        {
            // Subtle breathing/bobbing
            float bob = Mathf.Sin(animationTime * IdleBobSpeed) * IdleBobAmount;

            // Apply to visuals root
            if (visualsRoot != null)
            {
                Vector3 pos = visualsRoot.localPosition;
                pos.y = bob;
                visualsRoot.localPosition = pos;
                visualsRoot.localRotation = Quaternion.identity;
            }

            // Reset limbs to neutral
            if (leftArm != null) leftArm.localRotation = Quaternion.identity;
            if (rightArm != null) rightArm.localRotation = Quaternion.identity;
            if (leftLeg != null) leftLeg.localRotation = Quaternion.identity;
            if (rightLeg != null) rightLeg.localRotation = Quaternion.identity;

            // Subtle head movement
            if (head != null)
            {
                float headTilt = Mathf.Sin(animationTime * 0.5f) * 2f;
                head.localRotation = Quaternion.Euler(0, headTilt, 0);
            }
        }

        private void AnimateWalking()
        {
            float walkCycle = animationTime * WalkSpeed;

            // Walking bob
            float bob = Mathf.Abs(Mathf.Sin(walkCycle)) * WalkBobAmount;

            // Apply to visuals root with lean
            if (visualsRoot != null)
            {
                Vector3 pos = visualsRoot.localPosition;
                pos.y = bob;
                visualsRoot.localPosition = pos;

                // Slight forward lean when moving
                visualsRoot.localRotation = Quaternion.Euler(LeanAmount * currentSpeed, 0, 0);
            }

            // Arm swing (opposite to legs)
            float armSwing = Mathf.Sin(walkCycle) * ArmSwingAmount * currentSpeed;
            if (leftArm != null)
            {
                leftArm.localRotation = Quaternion.Euler(armSwing, 0, 0);
            }
            if (rightArm != null)
            {
                rightArm.localRotation = Quaternion.Euler(-armSwing, 0, 0);
            }

            // Leg swing
            float legSwing = Mathf.Sin(walkCycle) * LegSwingAmount * currentSpeed;
            if (leftLeg != null)
            {
                leftLeg.localRotation = Quaternion.Euler(-legSwing, 0, 0);
            }
            if (rightLeg != null)
            {
                rightLeg.localRotation = Quaternion.Euler(legSwing, 0, 0);
            }

            // Head stays relatively stable but with slight bob
            if (head != null)
            {
                head.localRotation = Quaternion.Euler(0, 0, 0);
            }
        }

        /// <summary>
        /// Repositions the FirePoint to the robot's chest/hand area
        /// </summary>
        public void RepositionFirePoint()
        {
            Transform firePoint = transform.Find("FirePoint");
            if (firePoint != null)
            {
                // Position at chest level, slightly forward
                firePoint.localPosition = new Vector3(0, 0.15f * robotScale, 0.2f * robotScale);
            }
        }

        /// <summary>
        /// Static method to ensure robot visuals are installed on the player
        /// </summary>
        public static void EnsureRobotVisuals(GameObject player)
        {
            if (player == null) return;

            PlayerVisualsInstaller installer = player.GetComponent<PlayerVisualsInstaller>();
            if (installer == null)
            {
                installer = player.AddComponent<PlayerVisualsInstaller>();
            }
        }
    }
}
