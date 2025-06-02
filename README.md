# TableTennisAI

version: **Unity6000.0.41f1**

A project that tried creating an AI that plays table tennis using MLAgents.



## Stage 1: Table tennis skill training 

<video src="https://github.com/user-attachments/assets/00fe5283-2fe2-4836-8670-c1e6c5b58312" controls width="600"></video>

## Stage 2: Cooperative Rally training

<video src="https://github.com/user-attachments/assets/0707e58d-5245-47bd-846f-0c2647e8d4c9" controls width="600"></video>

# RL Training Methodology

This study uses a curriculum learning approach with two stages. Both stages share a unified observation and action space, model architecture, and PPO training configuration.

## Shared Settings

- **Unity Version**: `6000.0.41f1`
- **ML-Agents Version**: `3.0.0`
- **Step interval**: `0.02s` (50 steps per second)

### Observation (23D)
- Ball position (3D), velocity (3D), angular velocity (3D)
- Racket position (3D), rotation in sin/cos (6D)
- Racket–Ball relative displacement (3D)
- All relative to a fixed anchor frame
- Control flags: `isServing`, `isHitable` (2D)

### Actions (6D)
- Racket translation: `(x, y, z)`
- Racket rotation: `(pitch, yaw, roll)`

### PPO Training Settings
- **Model Architecture**: 3 hidden layers, 1024 units each
- **Batch size**: `2048`, **Buffer size**: `20480`
- **Learning rate**: `0.0002` (linear decay)
- **Discount factor (γ)**: `0.995`, **GAE λ**: `0.95`
- **Clipping ε**: `0.2`, **Entropy β**: `0.001`
- **Epochs**: `4`, **Time-Horizon**: `1000`

### Self-play Parameters
- Save opponent every `50,000` steps
- Swap opponent every `5,000` steps (window = 10)
- Save champion every `200,000` steps (used as latest snapshot)
- **ELO** rating enabled  
- **Initial ELO**: `1200`

---

## Stage 1 – Skill Acquisition

- A single agent learns to serve and return the ball under two randomized conditions:
  - 50% of episodes: The agent serves the ball from rest.
  - 50% of episodes: The agent returns a ball launched from a scripted machine.

### Reward (Stage 1)
- `+1.0` for paddle contact, `+2.0` for valid bounce
- `–0.15` for dropping, `–0.3` for wrong bounce
- `–0.02` out-of-bounds, `–0.1` for idle

---

## Stage 2 – Cooperative Rally Building

### Reward (Stage 2)

#### First bounce (`bounceCount == 1`)
- `+5.0` if opponent side directly on serve (serve validated)
- `–5.0` if hits same side

#### Second bounce (`bounceCount == 2`)
- `+5.0` if second bounce is on opponent’s side (save rally)
- `–5.0` if second bounce is invalid

#### Third (or later) bounce (`bounceCount >= 3`)
- `+10.0` if opponent fails to return (hits twice on same side or out-of-bounds)
- `–5.0` if ball is hit and hit in rally (still alive)
- `–10.0` if the bounce lands on own side
