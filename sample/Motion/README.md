# Motion sample

An interactive sample for `Firelight.Motion` and `@lit-labs/motion`.

- **Reorder cards:** keyed `repeat` preserves elements while `Motion.animate` animates their positions.
- **Toggle notice:** `Motion.fadeIn` and `Motion.fadeOut` animate entry and exit.
- **Pause / play:** `AnimateController` pauses active animations and starts later `animate()` animations paused. Pause, rotate the cards, then play to see the transition. A controller applies to every `animate()` rendered by its host, so the layout demo is a separate `motion-layout-demo` element to keep pause / play away from the notice. Reorder and notice controls each wait until their own transition finishes so animations cannot stack on the same elements.
- **Move dot:** `SpringController` drives the dot between two stops on the track. The stops are percentages of the track width, so the dot overshoots and settles at each one, including on narrow screens.

Run `npm install` and `npm run dev` from this directory, then open the Vite URL.
