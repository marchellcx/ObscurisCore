using LabExtended.Utilities.Update;

using UnityEngine;

namespace ObscurisCore.Utilities
{
    /// <summary>
    /// Provides animation logic for launching and moving an object of type T using configurable direction, speed,
    /// duration, and gravity settings.
    /// </summary>
    /// <remarks>Use this class to animate objects along a trajectory with customizable physics parameters.
    /// The animation behavior is controlled via delegates for retrieving and updating the object's position, as well as
    /// an action to invoke upon completion. Ensure all required properties are set before starting the animation. This
    /// class implements IDisposable; call Dispose to release resources and stop the animation when it is no longer
    /// needed. Instances are not thread-safe.</remarks>
    /// <typeparam name="T">The type of the object to be animated.</typeparam>
    public class LaunchAnimator<T> : IDisposable
    {
        private bool disposed;
        private Vector3 currentVelocity;

        /// <summary>
        /// Gets or sets the delegate used to retrieve the current Vector3 value for a given object of type T.
        /// </summary>
        /// <remarks>Assign this property to specify how the current value should be obtained from an
        /// instance of T. The delegate should return the appropriate Vector3 representing the current state or position
        /// as needed by the consuming logic.</remarks>
        public Func<T, LaunchAnimator<T>, Vector3> GetCurrent { get; set; }

        /// <summary>
        /// Gets or sets the delegate used to update the current value of the object based on a specified position.
        /// </summary>
        /// <remarks>Assign this property to provide custom logic for updating the object's state when a
        /// new position is specified. The delegate receives the target object and the new position as parameters. This
        /// property is typically used to control how the object responds to position changes, such as during animations
        /// or user interactions.</remarks>
        public Action<T, LaunchAnimator<T>, Vector3> SetCurrent { get; set; }

        /// <summary>
        /// Gets or sets the action to invoke when the animation completes.
        /// </summary>
        /// <remarks>The action receives the animated value and the associated animator as parameters.
        /// Assign this property to execute custom logic after the animation finishes.</remarks>
        public Action<T, LaunchAnimator<T>> OnComplete { get; set; }

        /// <summary>
        /// Gets the current velocity vector of the object.
        /// </summary>
        public Vector3 Velocity => currentVelocity;

        /// <summary>
        /// Gets or sets the direction vector.
        /// </summary>
        public Vector3 Direction { get; set; }

        /// <summary>
        /// Gets or sets the speed value.
        /// </summary>
        public float Speed { get; set; } = 1f;

        /// <summary>
        /// Gets or sets the duration, in seconds, for the associated operation or event.
        /// </summary>
        public float Duration { get; set; } = 10f;

        /// <summary>
        /// Gets or sets the acceleration due to gravity used in physics calculations.
        /// </summary>
        public float Gravity { get; set; } = 9.81f;

        /// <summary>
        /// Gets or sets the target object associated with this instance.
        /// </summary>
        public T Target { get; set; }

        /// <summary>
        /// Begins animating the target object using the configured direction, speed, and duration settings.
        /// </summary>
        /// <remarks>Ensure that all required properties, such as GetCurrent, SetCurrent, Direction,
        /// Speed, Duration, and Target, are properly set before calling this method. Calling Start multiple times
        /// without stopping may result in multiple update subscriptions.</remarks>
        /// <exception cref="ObjectDisposedException">Thrown if the animator has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if required properties are not set before starting the animator, including when GetCurrent or
        /// SetCurrent is null, Direction is a zero vector, Speed or Duration is less than or equal to zero, or Target
        /// is null.</exception>
        public void Start() 
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(LaunchAnimator<>));

            if (GetCurrent == null)
                throw new InvalidOperationException("GetCurrent function must be set before starting the animator.");

            if (SetCurrent == null)
                throw new InvalidOperationException("SetCurrent function must be set before starting the animator.");

            if (OnComplete == null)
                throw new InvalidOperationException("OnComplete action must be set before starting the animator.");

            if (Direction == Vector3.zero)
                throw new InvalidOperationException("Direction must be a non-zero vector.");

            if (Speed <= 0f)
                throw new InvalidOperationException("Speed must be greater than zero.");

            if (Duration <= 0f)
                throw new InvalidOperationException("Duration must be greater than zero.");

            if (Target == null)
                throw new InvalidOperationException("Target must be set before starting the animator.");

            currentVelocity = Direction.normalized * Speed;

            PlayerUpdateHelper.OnLateUpdate += Update;
        }

        /// <summary>
        /// Releases all resources used by the instance and unsubscribes from update events.
        /// </summary>
        /// <remarks>Call this method when the instance is no longer needed to free resources and prevent
        /// further updates. After calling this method, the instance should not be used.</remarks>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            SetCurrent = null!;
            GetCurrent = null!;
            OnComplete = null!;

            Direction = Vector3.zero;
            currentVelocity = Vector3.zero;

            Speed = 0f;
            Gravity = 0f;
            Duration = 0f;

            Target = default!;

            PlayerUpdateHelper.OnLateUpdate -= Update;
        }

        private void Update()
        {
            Duration -= Time.deltaTime;

            if (Duration <= 0f)
            {
                OnComplete?.Invoke(Target, this);

                Dispose();
                return;
            }

            currentVelocity += Vector3.down * Gravity * Time.deltaTime;

            var currentPosition = GetCurrent(Target, this);
            var newPosition = currentPosition + currentVelocity * Time.deltaTime;

            SetCurrent(Target, this, newPosition);
        }
    }
}