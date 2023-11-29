<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import HelloWorld from './components/HelloWorld.vue'
import type { IStatusData } from "./fetchy.js";
import { useLoginStore } from './stores/login';
import type { ILoginState } from "./stores/login";
import { onMounted, ref } from 'vue';

// For each page, or every so often we want to update the login status...
// NOTE: The back-end of the application is responsible for evauluating the
// permissions of the current user.
const _Login = useLoginStore();
//const loginState = ref(_Login.GetState());


onMounted(() => {
  _Login.CheckLogin();
});

</script>


<template>
  <header class="login-header">
    <div v-if="_Login.IsLoggedIn">
      <img :src="_Login.Avatar" alt="avatar image" />
      <p>{{ _Login.DisplayName }}</p>
      <button class="link" @click="_Login.Logout">Log Out</button>
    </div>
    <div v-else>
      <p>Not Logged In</p>
    </div>
  </header>

  <main>
    <div class="title">
      <h1>MemberMan VUE Example</h1>
      <nav>
        <RouterLink to="/">Home</RouterLink>
        <RouterLink to="/register">Register</RouterLink>
      </nav>
    </div>

    <div class="content">
      <RouterView />
    </div>

    <div class="todo">
      <h2>TODO</h2>

      <h3>Back End</h3>
      <h3>Front-end</h3>
      <ul>
        <li>Complete UI for a logged in user.</li>
        <li>Forgot Password?</li>
        <li>Logout</li>
        <li>The register page needs to be updated to use EZForm?</li>
      </ul>
    </div>

  </main>

  <footer>

  </footer>
</template>

<style lang="less">
@linkColor: #0000FF;

header {
  line-height: 1.5;
  max-height: 100vh;
}

h1,
h2,
h3,
h4,
h5,
h6 {
  text-align: center;
}

@siteWidth: 1500px;
.content {
  max-width: 1500px;

  > * { 
    display:block;
    margin: 0 auto;
    max-width: 1500px;
  }
}

a.as-link {
  text-decoration: underline;
  color: @linkColor;
  cursor: pointer;
}


.ez-form {
  text-align: center;
}


button.link-button {
  background: none;
  border: none;
  text-decoration: underline;

  color: @linkColor;
  margin: 0;
  padding: 0;
}

button.link-button:hover {
  cursor: pointer;
}

.todo {
  margin-top: 2rem;
  border: solid 1px pink;
  padding: 1rem;
  text-align: left !important;

  h1,
  h2,
  h3,
  h4,
  h5,
  h6 {
    text-align: initial;
  }

}
</style>
