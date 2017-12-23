using System;
using Assets.Gamelogic.Utils;
using Improbable.Core;
using Improbable.Fire;
using Improbable.Unity;
using Improbable.Unity.Visualizer;
using UnityEngine;

namespace Assets.Gamelogic.Core
{
    [WorkerType(WorkerPlatform.UnityClient)]
    public class TransformReceiverClientControllableAuthoritative : MonoBehaviour
    {
        [Require] private ClientAuthorityCheck.Writer clientAuthorityCheck;
        [Require] private TransformComponent.Reader transformComponent;
        [Require] private Flammable.Reader flammable;

        private Vector3 targetVelocity;
        private Vector3 mousePosition;
        [SerializeField] private Rigidbody myRigidbody;
        

        private void Awake()
        {
            myRigidbody = gameObject.GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            transformComponent.ComponentUpdated.Add(OnTransformComponentUpdated);
        }

        private void OnDisable()
        {
            transformComponent.ComponentUpdated.Remove(OnTransformComponentUpdated);
        }

        private void OnTransformComponentUpdated(TransformComponent.Update update)
        {
            for (int i = 0; i < update.teleportEvent.Count; i++)
            {
                TeleportTo(update.teleportEvent[i].targetPosition.ToVector3());
            }
        }

        private void TeleportTo(Vector3 position)
        {
            myRigidbody.velocity = Vector3.zero;
            myRigidbody.MovePosition(position);
        }

        public void SetTargetVelocity(Vector3 direction)
        {
            bool isOnFire = flammable != null && flammable.Data.isOnFire;
            var movementSpeed = SimulationSettings.PlayerMovementSpeed * (isOnFire ? SimulationSettings.OnFireMovementSpeedIncreaseFactor : 1f);
            targetVelocity = direction * movementSpeed;
        }

        private void FixedUpdate()
        {
            MovePlayer();
            RotatePlayer();
        }

        public void RotatePlayer()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                //Vector3 newMousePos = new Vector3(hit.point.x, 0, hit.point);
                transform.LookAt(hit.point);
                transform.Rotate(90, 0, 180);
            } 
        }

        internal void SetLookDirection(Vector3 mousePos)
        {
            mousePosition = mousePos;
        }

        public void MovePlayer()
        {
            var currentVelocity = myRigidbody.velocity;
            
            var velocityChange = targetVelocity - currentVelocity;
            if (ShouldMovePlayerAuthoritativeClient(velocityChange))
                
            {
                Vector3 dvector = velocityChange + targetVelocity;
                dvector.x = 90f;
                //transform.LookAt(dvector);
                myRigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
            }
        }

        private bool ShouldMovePlayerAuthoritativeClient(Vector3 velocityChange)
        {
            return velocityChange.sqrMagnitude > Mathf.Epsilon && PlayerMovementCheatSafeguardPassedAuthoritativeClient(velocityChange);
        }

        private bool PlayerMovementCheatSafeguardPassedAuthoritativeClient(Vector3 velocityChange)
        {
            var result = velocityChange.sqrMagnitude < SimulationSettings.PlayerPositionUpdateMaxSqrDistance;
            if (!result)
            {
                Debug.LogError("Player movement cheat safeguard failed on Client. " + velocityChange.sqrMagnitude);
            }
            return result;
        }
    }
}
