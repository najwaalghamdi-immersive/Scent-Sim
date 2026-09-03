'use strict';

// Multi-port training scenarios: real-world situations where more than one
// cartridge would plausibly fire in the same scene. Each scenario fires its
// ports in order with a short stagger, since the generator has one fan and
// activates cartridges one command at a time (see the user guide's API
// section) — this is a short script of individual OUT commands, not a
// blended scent.
module.exports = [
  {
    id: 'vehicle-collision',
    name: 'Vehicle Collision',
    description: 'Traffic-collision response: injury, scorched tires, and a fuel spill at the scene.',
    ports: [1, 4, 7] // Blood, Burnt Rubber, Gasoline
  },
  {
    id: 'structure-fire',
    name: 'Structure Fire',
    description: 'A building fire with melting synthetics and scorched fittings.',
    ports: [2, 5, 4] // Fire & Explosions, Burnt Plastic, Burnt Rubber
  },
  {
    id: 'active-shooter',
    name: 'Active Shooter Response',
    description: 'Tactical entry training: spent gunpowder over a casualty.',
    ports: [3, 1] // Gunfire & Gunpowder, Blood
  },
  {
    id: 'volcanic-eruption',
    name: 'Volcanic Eruption',
    description: 'Geoscience and natural-disaster training: sulfurous gas and combustion.',
    ports: [10, 2] // Volcano, Fire & Explosions
  },
  {
    id: 'aircraft-incident',
    name: 'Aircraft Incident',
    description: 'Airport emergency response: aviation fuel, fire, and scorched rubber on the tarmac.',
    ports: [9, 2, 4] // Aviation Gas, Fire & Explosions, Burnt Rubber
  },
  {
    id: 'fuel-depot-spill',
    name: 'Fuel Depot Spill',
    description: 'Industrial-site hazmat training: three fuel types pooling together.',
    ports: [7, 8, 6] // Gasoline, Diesel, Burnt Car Oil
  }
];
