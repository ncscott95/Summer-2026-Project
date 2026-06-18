using UnityEngine;
using System.Collections;

public class RopeDartInputController : Singleton<RopeDartInputController>
{
    [SerializeField] private RopeDartControlData data;

    private Coroutine dartDirectionBufferCoroutine;
    private bool isBufferingDartDirection = false;
    private Vector2 lastDartDirectionInput = Vector2.zero;

    public void HandleSpinRetrieveInput()
    {
        RopeDartManager.Instance.ToggleTryingToSpin(true);

        if (RopeDartManager.Instance.CurrentState == RopeDartState.Idle || RopeDartManager.Instance.CurrentState == RopeDartState.Stalling) 
        {
            RopeDartManager.Instance.StartSpin();
        }
        else if (RopeDartManager.Instance.CurrentState == RopeDartState.Extended || RopeDartManager.Instance.CurrentState == RopeDartState.Casting) 
        {
            RopeDartManager.Instance.Retrieve();
        }
    }

    public void HandleSpinInputEnd()
    {
        RopeDartManager.Instance.ToggleTryingToSpin(false);

        if (RopeDartManager.Instance.CurrentState == RopeDartState.Spinning) RopeDartManager.Instance.StopSpin();
    }

    public void HandleCastInput()
    {
        if (isBufferingDartDirection)
        {
            // TODO: cast with modifier based on lastDartDirectionInput
            StopCoroutine(dartDirectionBufferCoroutine);
            dartDirectionBufferCoroutine = null;
        }

        dartDirectionBufferCoroutine = StartCoroutine(DartDirectionBufferCoroutine(2));
    }

    public void HandleTwineInput()
    {
        // if (isBufferingDartDirection && lastDartDirectionInput != Vector2.zero)
        // {
        //     // TODO: twine with modifier based on lastDartDirectionInput
        //     StopCoroutine(dartDirectionBufferCoroutine);
        //     dartDirectionBufferCoroutine = null;
        // }

        // dartDirectionBufferCoroutine = StartCoroutine(DartDirectionBufferCoroutine(1));
        
        // TODO: TEMPORARY
        RopeDartManager.Instance.TwineSimple();
    }

    public void HandleWrapInput()
    {
        
    }

    public void HandleWrapInputEnd()
    {
        
    }

    public void HandleDartDirectionInput(Vector2 input)
    {
        if (input.magnitude < 0.5f)
            return;

        if (isBufferingDartDirection)
        {
            // TODO: modify buffered twine or cast
            StopCoroutine(dartDirectionBufferCoroutine);
            dartDirectionBufferCoroutine = null;
        }

        lastDartDirectionInput = input;
        dartDirectionBufferCoroutine = StartCoroutine(DartDirectionBufferCoroutine(0));
    }

    // startedBy: 0 = direction change, 1 = twine, 2 = cast
    private IEnumerator DartDirectionBufferCoroutine(int startedBy)
    {
        isBufferingDartDirection = true;
        yield return new WaitForSeconds(data.DartDirectionBufferTime);
        lastDartDirectionInput = Vector2.zero;
        isBufferingDartDirection = false;

        switch (startedBy)
        {
            // twine without direction input
            case 0:
                RopeDartManager.Instance.TwineSimple();
                break;

            // change spin orientation
            case 1:
                break;

            // cast with no modifier
            case 2:
                RopeDartManager.Instance.Cast();
                break;

            default:
                break;
        }

        dartDirectionBufferCoroutine = null;
    }
}
