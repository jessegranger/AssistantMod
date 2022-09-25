using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using static Assistant.Globals;

namespace Assistant {
	public abstract class State {

		// 'Next' defines the default State we will go to when this State is complete.
		// This value is just a suggestion, the real value is what gets returned by OnTick
		public State Next = null;
		// 'Fail' defines the State to go to if there is any kind of exception
		public State Fail = null;

		public State(State next = null) => Next = next;

		// OnEnter gets called once (by a StateMachine) before the first call to OnTick.
		public virtual State OnEnter() => this;

		// OnTick gets called every frame (by a StateMachine), and should return the next State to continue with (usually itself).
		public virtual State OnTick() => this;

		// OnCancel gets called (by a StateMachine), to ask a State to clean up any incomplete work immediately (before returning).
		public virtual void OnCancel() { }

		public virtual State Then(State next) {
			Next = next;
			return lastNext();
		}
		public virtual State Then(params State[] next) {
			State cursor = this;
			foreach ( State s in next ) {
				cursor = cursor.Then(s);
			}
			return cursor.lastNext();
		}
		public virtual State Then(Action action) {
			Next = State.From(action);
			return lastNext();
		}
		private State lastNext() {
			if ( Next == null ) return this;
			else return Next.lastNext();
		}

		// A friendly name for the State, the class name by default.
		public virtual string Name => GetType().Name.Split('.').Last();

		// A verbose description of the State, that includes the Name of the next State (if known).
		public override string ToString() => $"{Name}{(Next == null ? "" : " then " + Next.Name)}";
		public virtual string Describe() => $"{Name}{(Next == null ? " end" : " then " + Next.Describe())}";

		// You can create a new State using any Func<State, State>
		public static State From(string label, Func<State, State> func) => new Runner(label, func);
		public static State From(Func<State, State> func) => new Runner(func);
		public static State From(Action func) => new ActionState(func);
		// public static implicit operator State(Func<State, State> func) => new Runner(func);
		// public static implicit operator Func<State, State>(State state) => (s) => { try { return state.OnTick(); } catch ( Exception ) { return null; } };

		// A Runner is a special State that uses a Func<State, State> to convert the Func into a class object with a State interface
		private class Runner : State {
			readonly Func<State, State> F;
			public Runner(Func<State, State> func) => F = func;
			public override State OnTick() => F(this);
			public override string Name => name;
			private readonly string name = "...";
			public Runner(string name, Func<State, State> func) : this(func) => this.name = name;
		}

		public class Machine : State {

			// each machine runs any number of states at once (in 'parallel' frame-wise)
			// when a machine is empty, it gets collected by the reaper
			public LinkedList<State> States;

			public State CurrentState => States.FirstOrDefault();

			// public Machine(params Func<State, State>[] states) : this(states.Cast<State>().ToArray()) { }
			public Machine(params State[] states) => States = new LinkedList<State>(states);

			public override string ToString() => string.Join(" while ", States.Select(s => $"({s})")) + (Next == null ? "" : $" then {Next.Name}");

			private static LinkedListNode<T> RemoveAndContinue<T>(LinkedList<T> list, LinkedListNode<T> node) {
				LinkedListNode<T> next = node.Next;
				list.Remove(node);
				return next;
			}

			/// <summary>
			/// Clear is a special state that clears all running states from a MultiState.
			/// </summary>
			/// <example>new StateMachine(
			///   new WalkTo(X),
			///   new ShootAt(Y, // will walk and shoot at the same time, when shoot is finished, clear the whole machine (cancel the walk)
			///     new StateMachine.Clear(this)) );
			///  </example>
			public class Clear : State {
				public Clear(State next) : base(next) { }
				public override State OnTick() => Next;
			}

			private Action<string> logDelegate;
			public void EnableLogging(Action<string> logger) => logDelegate = logger;
			public void DisableLogging() => logDelegate = null;
			private void Log(string s) => logDelegate?.Invoke($"{machineTimer.Elapsed} {s}");

			private static Stopwatch machineTimer = new Stopwatch();
			static Machine() => machineTimer.Start();

			public void Pause() => Paused = true;
			public void Resume() => Paused = false;
			public void TogglePause() => Paused = !Paused;
			private bool Paused = false;

			public override State OnTick() {
				if ( Paused ) return this;
				// Each State in the States list will be ticked "in parallel" (all get ticked each frame)
				Stopwatch watch = new Stopwatch();
				watch.Start();
				try {
					// iterate over the linked list of currently active states:
					LinkedListNode<State> curNode = States.First;
					while ( curNode != null ) {
						try {
							// each node in the linked list contains one State
							State curState = curNode.Value;
							// that state is ticked once per frame
							Stopwatch thisTick = Stopwatch.StartNew();
							State gotoState = curState.OnTick();
							long elapsed = thisTick.ElapsedMilliseconds;
							if( elapsed > 100 ) {
								Log($"OnTick: {curState.Name} took {elapsed} ms, cancelling...");
								gotoState = null;
							}

							// the result can either terminate, replace, or continue, the State in this node
							if ( gotoState == null ) { // terminate the State in this node
								Log($"State Finished: {curState.Name}.");
								curNode = RemoveAndContinue(States, curNode); // unlink from the linked list
								continue;
							}
							if ( gotoState != curState ) { // replace the State in this node with gotoState
								gotoState = gotoState.OnEnter();
								Log($"State Changed: {curState.Name} to {gotoState.Name}");
								if ( gotoState.GetType() == typeof(Clear) ) {
									Cancel(except: curState); // call all OnAbort in State, except curState.OnAbort, because it just ended cleanly (as far as it knows)
									return gotoState.Next ?? Next;
								}
								curState.Next = null; // just in case
								curNode.Value = gotoState;
							}
						} catch( Exception e ) {
							DebugWindow.LogError(e.StackTrace);
						}
						curNode = curNode.Next; // loop over the whole list
					}
				} finally {
					watch.Stop();
					long elapsed = watch.ElapsedMilliseconds;
					if( elapsed > 30 ) {
						// Log($"StateMachine: {GetType().Name} [{string.Join(", ", States.Select((s) => s.ToString()))}] {watch.Elapsed}");
						// DrawTextAtPlayer($"StateMachine: {GetType().Name} [{string.Join(", ", States.Select((s) => s.ToString()))}] {watch.Elapsed}");
					}
				}
				return States.Count == 0 ? Next : this;
			}
			public void Cancel(State except = null) {
				foreach ( State s in States ) if ( s != except ) s.OnCancel();
				States.Clear();
			}
			public void Add(State state) {
				if ( state == null ) return;
				Log($"State Added: {state.Name}");
				States.AddLast(States.Count == 0 ? state.OnEnter() : state);
			}
			public void Remove(State state) => States.Remove(state);
			public void Remove(Type stateType) {
				LinkedListNode<State> cur = States.First;
				while ( cur != null ) {
					cur = cur.Value.GetType() == stateType ? RemoveAndContinue(States, cur) : cur.Next;
				}
			}

			public bool HasState(Type stateType) => States.Any(s => s.GetType() == stateType);
			public bool HasState(string stateName) => States.Any(s => s.Name.Equals(stateName));
		}

		public static State WaitFor(uint duration, Func<bool> predicate, State next, State fail) {
			Stopwatch timer = Stopwatch.StartNew();
			return State.From((state) =>
				timer.ElapsedMilliseconds > duration ? fail :
					predicate() ? next :
					state
			);
		}
		private class ActionState : State {
			public readonly Action Act;
			public ActionState(Action action, State next = null) : base(next) => Act = action;
			public override State OnTick() {
				Act?.Invoke();
				return Next;
			}
		}
	}

	public class Delay : State // Delay is a State that waits for a fixed number of milliseconds
	{
		private static Stopwatch sw = Stopwatch.StartNew(); // use only one timer for all the (many) Delay objects
		private long started;
		readonly uint ms;
		public Delay(uint ms, State next = null) : base(next) => this.ms = ms;
		public override State OnEnter() {
			started = sw.ElapsedMilliseconds;
			return this;
		}
		public override State OnTick() => (sw.ElapsedMilliseconds - started) >= ms ? Next : (this);
		public override string Name => $"Delay({ms})";
	}

	class InputState : State {
		protected readonly static bool debug = false;
		protected InputState(State next = null) : base(next) { }
		public override State OnTick() => Next;
	}

	class KeyState : InputState {
		public readonly Keys Key;
		protected KeyState(Keys key, State next = null) : base(next) => Key = key;
	}

	class KeyDown : KeyState {
		public KeyDown(Keys key, State next = null) : base(key, next) { }
		public override State OnTick() {
			if ( !AllowInputInChatBox && ChatIsOpen() ) return Next;
			if ( debug ) Log($"KeyDown {Key}");
			InputSimulator.KeyDown(Key);
			return Next;
		}
		public override string Name => $"KeyDown({Key})";
	}

	class KeyUp : KeyState {
		public KeyUp(Keys key, State next = null) : base(key, next) { }
		public override State OnTick() {
			if ( !AllowInputInChatBox && ChatIsOpen() ) return Next;
			if ( debug ) Log($"KeyUp {Key}");
			InputSimulator.KeyUp(Key);
			return Next;
		}
		public override string Name => $"KeyUp({Key})";
	}

	class PressKey : KeyState {
		private static Stopwatch pressTimer = Stopwatch.StartNew();
		private static Dictionary<Keys, long> lastPressTime = new Dictionary<Keys, long>();
		private readonly long throttle = long.MaxValue;
		public PressKey(Keys key, uint duration, State next = null) : base(key,
				new KeyDown(key, new Delay(duration, new KeyUp(key, next)))) { }
		public PressKey(Keys key, uint duration, long throttle, State next = null) : base(key,
				new KeyDown(key, new Delay(duration, new KeyUp(key, next)))) {
			this.throttle = throttle;
		}

		public override State OnEnter() {
			lastPressTime.TryGetValue(Key, out long lastPress);
			long now = pressTimer.ElapsedMilliseconds;
			long elapsed = now - lastPress;
			if( throttle != long.MaxValue && elapsed < throttle ) {
				Log($"Key {Key} throttled. {elapsed} < {throttle}");
				return null;
			}
			lastPressTime[Key] = pressTimer.ElapsedMilliseconds;
			return Next;
		}
	}

	class MoveMouse : InputState {
		public readonly float X;
		public readonly float Y;
		public MoveMouse(float x, float y, State next = null) : base(next) {
			X = x; Y = y;
		}
		public MoveMouse(Vector2 pos, State next = null) : this(pos.X , pos.Y, next) { }
		public MoveMouse(Element label, State next = null) : this(label?.GetClientRect().Center ?? Vector2.Zero, next) { }
		public override State OnTick() {
			if ( X == 0 && Y == 0 ) {
				Log($"Warn: MoveMouse to (0,0) attempted, skipped.");
				return Next;
			}
			InputSimulator.MouseMoveTo(X, Y);
			if ( debug ) Log($"MoveMouse: {X} {Y}");
			// Input.SetCursorPos(new Vector2(X, Y));
			// input.Mouse.MoveMouseTo(
					// (w.Left + X) * 65535 / bounds.Width,
					// (w.Top + Y) * 65535 / bounds.Height);
			return Next;
		}
		public static MoveMouse SnapToGroundLabel(State next = null) {
			var mousePos = Input.MousePosition;
			var nearestToMouse = GroundLabels()
				.Where(IsValid)
				.OrderBy(label => Vector2.DistanceSquared(label.Label.GetClientRect().Center, mousePos))
				.FirstOrDefault();
			if ( nearestToMouse == null ) return null;
			return new MoveMouse(nearestToMouse.Label, next);
		}
	}
	class LeftMouseDown : InputState {
		public LeftMouseDown(State next = null) : base(next) { }
		public override State OnTick() {
			if ( debug ) Log($"LeftMouseDown");
			InputSimulator.LeftDown();
			return Next;
		}
	}

	class LeftMouseUp : InputState {
		public LeftMouseUp(State next = null) : base(next) { }
		public override State OnTick() {
			if ( debug ) Log($"LeftMouseUp");
			InputSimulator.LeftUp();
			return Next;
		}
	}

	class LeftClick : InputState {
		public LeftClick(uint duration, uint count, State next = null) : base(
			count == 0 ? next
			: new LeftMouseDown(new Delay(duration, new LeftMouseUp(
				count > 1 ? new Delay(100, new LeftClick(duration, count - 1, next))
				: next)))) { }
		public override State OnEnter() => Next;
	}

	class LeftClickAt : InputState {
		public LeftClickAt(Element elem, uint duration, uint count, State next = null) : this(elem?.GetClientRect().Center ?? Vector2.Zero, duration, count, next) { }
		public LeftClickAt(Vector2 pos, uint duration, uint count, State next = null) : this(pos.X, pos.Y, duration, count, next) { }
		public LeftClickAt(float x, float y, uint duration, uint count, State next = null) : base(
				new MoveMouse(x, y, new Delay(duration, new LeftClick(duration, count, next)))) { }
	}

	class RightMouseDown : InputState {
		public RightMouseDown(State next = null) : base(next) { }
		public override State OnTick() {
			if ( debug ) Log($"RightButtonDown");
			InputSimulator.RightDown();
			return Next;
		}
	}

	class RightMouseUp : InputState {
		public RightMouseUp(State next = null) : base(next) { }
		public override State OnTick() {
			if ( debug ) Log($"RightButtonUp");
			InputSimulator.RightUp();
			return Next;
		}
	}

	class RightClick : InputState {
		public RightClick(uint duration, State next = null) : base(
				new RightMouseDown(new Delay(duration, new RightMouseUp(next)))) { }
		public override State OnEnter() => Next;
	}

	class RightClickAt : InputState {
		public RightClickAt(Element item, uint duration, State next = null) : this(item?.GetClientRect().Center ?? Vector2.Zero, duration, next) { }
		public RightClickAt(Vector2 pos, uint duration, State next = null) : this(pos.X, pos.Y, duration, next) { }
		public RightClickAt(float x, float y, uint duration, State next = null) : base(
				new MoveMouse(x, y, new Delay(duration, new RightMouseDown(new Delay(duration, new RightMouseUp(next)))))) { }
	}
	class CtrlRightClickAt : InputState {
		public CtrlRightClickAt(Element item, uint duration, State next = null) : this(item?.GetClientRect().Center ?? Vector2.Zero, duration, next) { }
		public CtrlRightClickAt(Vector2 pos, uint duration, State next = null) : this(pos.X, pos.Y, duration, next) { }
		public CtrlRightClickAt(float x, float y, uint duration, State next = null) : base(
				new MoveMouse(x, y, new Delay(duration, 
					new KeyDown(Keys.LControlKey, new Delay(duration,
						new RightMouseDown(new Delay(duration,
							new RightMouseUp(new Delay(duration,
								new KeyUp(Keys.LControlKey, next)))))))))) { }
	}
	class CtrlLeftClickAt : InputState {
		public CtrlLeftClickAt(Element item, uint duration, State next = null) : this(item?.GetClientRect().Center ?? Vector2.Zero, duration, next) { }
		public CtrlLeftClickAt(Vector2 pos, uint duration, State next = null) : this(pos.X, pos.Y, duration, next) { }
		public CtrlLeftClickAt(float x, float y, uint duration, State next = null) : base(
				new MoveMouse(x, y, new Delay(duration, 
					new KeyDown(Keys.LControlKey, new Delay(duration,
						new LeftMouseDown(new Delay(duration,
							new LeftMouseUp(new Delay(duration,
								new KeyUp(Keys.LControlKey, next)))))))))) { }
	}
	class ShiftLeftClickAt : InputState {
		public ShiftLeftClickAt(Element item, uint duration, State next = null) : this(item?.GetClientRect().Center ?? Vector2.Zero, duration, next) { }
		public ShiftLeftClickAt(Vector2 pos, uint duration, State next = null) : this(pos.X, pos.Y, duration, next) { }
		public ShiftLeftClickAt(float x, float y, uint duration, State next = null) : base(
				new MoveMouse(x, y, new Delay(duration, 
					new KeyDown(Keys.LShiftKey, new Delay(duration,
						new LeftMouseDown(new Delay(duration,
							new LeftMouseUp(new Delay(duration,
								new KeyUp(Keys.LShiftKey, next)))))))))) { }
	}

}
