import numpy as np
import random
import copy
import datetime
import platform
import torch
import torch.nn.functional as F
from torch.utils.tensorboard import SummaryWriter
from collections import deque
from mlagents_envs.environment import UnityEnvironment, ActionTuple
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel

# DDPG를 위한 파라미터 값 설정
state_size = 9
action_size = 3

load_model = False # 모델 적용 전
train_mode = True

# load_model = True # 모델 적용 후
# train_mode = False

batch_size = 128
mem_maxlen = 50000
discount_factor = 0.9
actor_lr = 1e-4
critic_lr = 5e-4
tau = 1e-3

# OU Noise 파라미터
mu = 0
theta = 1e-3
sigma = 2e-3

#run_step = 50000 if train_mode else 0 # 모델 적용 전
run_step = 5000 # 모델 적용 후
test_step = 10000
train_start_step = 5000

print_interval = 10
save_interval = 100

# 유니티 환경 경로 설정
game = "Drone"
os_name = platform.system()
# if os_name == "Windows":
#     env_name = f"../envs/{game}_{os_name}/{game}"
# elif os_name == "Darwin":
#     env_name = f"../envs/{game}_{os_name}"
if os_name == "Windows":
    env_name = f"/Users/jang-young-a/Desktop/캡스톤/유니티/4_28_Drone_Start_2/Drone.app"
elif os_name == "Darwin":
    env_name = f"/Users/jang-young-a/Desktop/캡스톤/유니티/4_28_Drone_Start_2/Drone.app"


# 모델 저장 및 불러오기 경로
date_time = datetime.datetime.now().strftime("%Y%m%d%H%M%S")
save_path = f"./saved_models/{game}/DDPG/{date_time}"
load_path = f"./saved_models/{game}/DDPG/20250429015340"

# 연산 장치 설정
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

# OU Noise 클래스
class OU_noise:
    def __init__(self):
        self.reset()
        
    def reset(self):
        self.X = np.ones((1, action_size), dtype=np.float32) * mu
        
    def sample(self):
        dx = theta * (mu - self.X) + sigma * np.random.randn(len(self.X))
        self.X += dx
        return self.X

# Actor 클래스
class Actor(torch.nn.Module):
    def __init__(self):
        super(Actor, self).__init__()
        self.fc1 = torch.nn.Linear(state_size, 128)
        self.fc2 = torch.nn.Linear(128, 128)
        self.mu = torch.nn.Linear(128, action_size)
        
    def forward(self, state):
        x = torch.relu(self.fc1(state))
        x = torch.relu(self.fc2(x))
        return torch.tanh(self.mu(x))

# Critic 클래스
class Critic(torch.nn.Module):
    def __init__(self):
        super(Critic, self).__init__()
        
        self.fc1 = torch.nn.Linear(state_size, 128)
        self.fc2 = torch.nn.Linear(128 + action_size, 128)
        self.q = torch.nn.Linear(128, 1)
    
    def forward(self, state, action):
        x = torch.relu(self.fc1(state))
        x = torch.cat((x, action), dim=-1)
        x = torch.relu(self.fc2(x))
        return self.q(x)

# DDPGAgent 클래스
class DDPGAgent():
    def __init__(self):
        self.actor = Actor().to(device)
        self.target_actor = copy.deepcopy(self.actor)
        self.actor_optimizer = torch.optim.Adam(self.actor.parameters(), lr=actor_lr)
        self.critic = Critic().to(device)
        self.target_critic = copy.deepcopy(self.critic)
        self.critic_optimizer = torch.optim.Adam(self.critic.parameters(), lr=critic_lr)
        self.OU = OU_noise()
        self.memory = deque(maxlen=mem_maxlen)
        self.writer = SummaryWriter(save_path)
        
        if load_model:
            checkpoint = torch.load(load_path + '/ckpt', map_location=device)
            self.actor.load_state_dict(checkpoint["actor"])
            self.target_actor.load_state_dict(checkpoint["actor"])
            self.actor_optimizer.load_state_dict(checkpoint["actor_optimizer"])
            self.critic.load_state_dict(checkpoint["critic"])
            self.target_critic.load_state_dict(checkpoint["critic"])
            self.critic_optimizer.load_state_dict(checkpoint["critic_optimizer"])
            
    def get_action(self, state, training=True):
        self.actor.train(training)
        action = self.actor(torch.FloatTensor(state).to(device)).cpu().detach().numpy()
        return action + self.OU.sample() if training else action
        
    def append_sample(self, state, action, reward, next_state, done):
        self.memory.append((state, action, reward, next_state, done))
    
    def train_model(self):
        batch = random.sample(self.memory, batch_size)
        state, action, reward, next_state, done = map(lambda x: torch.FloatTensor(np.stack([b[x] for b in batch])).to(device), range(5))
        
        next_actions = self.target_actor(next_state)
        next_q = self.target_critic(next_state, next_actions)
        target_q = reward + (1 - done) * discount_factor * next_q
        q = self.critic(state, action)
        critic_loss = F.mse_loss(target_q, q)

        self.critic_optimizer.zero_grad()
        critic_loss.backward()
        self.critic_optimizer.step()

        action_pred = self.actor(state)
        actor_loss = -self.critic(state, action_pred).mean()

        self.actor_optimizer.zero_grad()
        actor_loss.backward()
        self.actor_optimizer.step()

        return actor_loss.item(), critic_loss.item()

    def soft_update_target(self):
        for target_param, local_param in zip(self.target_actor.parameters(), self.actor.parameters()):
            target_param.data.copy_(tau * local_param.data + (1.0 - tau) * target_param.data)
        for target_param, local_param in zip(self.target_critic.parameters(), self.critic.parameters()):
            target_param.data.copy_(tau * local_param.data + (1.0 - tau) * target_param.data)

    def save_model(self):
        print(f"Saving model to {save_path}/ckpt")
        torch.save({
            "actor": self.actor.state_dict(),
            "actor_optimizer": self.actor_optimizer.state_dict(),
            "critic": self.critic.state_dict(),
            "critic_optimizer": self.critic_optimizer.state_dict(),
        }, save_path + '/ckpt')

if __name__ == '__main__':
    engine_configuration_channel = EngineConfigurationChannel()
    env = UnityEnvironment(file_name=None, side_channels=[engine_configuration_channel])
#    env = UnityEnvironment(file_name=None, side_channels=[engine_configuration_channel])

    try:
        env.reset()
        # ML-Agents 실행 코드 추가
    finally:
        env.close()  # 실행 후 Unity 환경 정상 종료
        
    agent = DDPGAgent()

    for step in range(run_step + test_step):
        if step == run_step:
            if train_mode:
                agent.save_model()
            train_mode = False
            engine_configuration_channel.set_configuration_parameters(time_scale=1.0)

    print("Training complete")
