<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import HelloWorld from './components/HelloWorld.vue'
import type { IStatusData } from "./fetchy.js";
import { useLoginStore } from './stores/login';
import type { ILoginState } from "./stores/login";
import { initCustomFormatter, ref } from 'vue';

import type { LoginResponse } from './stores/login';

const _Login = useLoginStore();

// For each page, or every so often we want to update the login status...
// NOTE: The back-end of the application is responsible for evauluating the
// permissions of the current user.
const loginState = ref(_Login.GetState());

// Form properties.
let username = "";
let password = "";
const isWorking = ref(false);
const isFormValid = ref(false);

async function tryLogin() {
  if (username != "" && password != "" && !isWorking.value) {
    isWorking.value = true;

    await _Login.Login(username, password).then((res) => {
      if (res.Error) {
        alert('some fuckin error.....');
      }
      else {
        const data: LoginResponse = res.Data!;
        if (res.Success && data?.IsLoggedIn) {
          alert('update the current login state!');
        }
        else {
          alert("login failed!" + data?.Message);
        }
      }

      isWorking.value = false;
    });
  }
  else {
    alert('login not available!');
  }
}


function updateForm() {
  // Revalidate the form.....
  const isValid = username != "" && password != "";
  isFormValid.value = isValid;

}

</script>



<template>
  <header class="login-header">
    <div v-if="loginState?.IsLoggedIn">
      <img :src="loginState.Avatar" alt="avatar image" />
      <p>{{ loginState.DisplayName }}</p>
      <button class="link" @click="_Login.Logout">Log Out</button>
    </div>
    <div v-else>
      <p>Not Logged In</p>
    </div>
  </header>

  <main>
    <div class="title">
      <!-- <HelloWorld msg="You did it!" /> -->
      <h1>Member Man Tester</h1>
      <nav>
        <RouterLink to="/">Home</RouterLink>
        <RouterLink to="/about">About</RouterLink>
      </nav>
    </div>

    <div :class="isWorking ? 'login working' : 'login'">
      <form>
        <div class="input">
          <input type="text" name="username" v-model="username" placeholder="Username" :disabled="isWorking" />
        </div>
        <div class="input">
          <input type="password" name="password" v-model="password" placeholder="Password" :disabled="isWorking"
            @input="updateForm" />
        </div>
        <button type="button" @click="tryLogin" :disabled="!isFormValid || isWorking">Login</button>
      </form>
    </div>
  </main>

  <footer>

  </footer>
  <!-- <RouterView /> -->
</template>

<style scoped>
header {
  line-height: 1.5;
  max-height: 100vh;
}

.login.working {
  background: red;
}
</style>
