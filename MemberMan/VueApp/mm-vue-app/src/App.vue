<script setup lang="ts">
import { RouterLink, RouterView, useRouter } from 'vue-router'
import HelloWorld from './components/HelloWorld.vue'
import type { IStatusData } from "./fetchy.js";
import { useLoginStore } from './stores/login';
import type { ILoginState } from "./stores/login";
import { onMounted, ref } from 'vue';

// For each page, or every so often we want to update the login status...
// NOTE: The back-end of the application is responsible for evauluating the
// permissions of the current user.
const _Login = useLoginStore();
const _Router = useRouter();

onMounted(() => {
  _Login.CheckLogin();
});

async function logout() {
  await _Login.Logout();

  // OPTIONS: HOME PAGE.
  _Router.push("/");
}

</script>


<template>
  <header class="login-header">
    <div class="title">
      <h1>MemberMan VUE Example</h1>
    </div>

    <div class="login-status">
      <div v-if="_Login.IsLoggedIn">
        <img :src="_Login.Avatar" alt="avatar image" />
        <p>{{ _Login.DisplayName }}</p>
        <button class="link" @click="logout">Log Out</button>
      </div>
      <div v-else>
        <p>Not Logged In</p>
      </div>
    </div>
  </header>

  <main>
    <nav>
      <RouterLink to="/">Home</RouterLink>
      <RouterLink v-if="!_Login.IsLoggedIn" to="/login">Login</RouterLink>
      <RouterLink v-if="!_Login.IsLoggedIn" to="/signup">Sign Up</RouterLink>
      <RouterLink v-if="_Login.IsLoggedIn" to="/account">Account</RouterLink>
    </nav>


    <div class="content">
      <RouterView />
    </div>

    <div class="todo">
      <h2>TODO</h2>

      <h3>Back End</h3>
        <li>Fetchy should be a functor type deal....</li>
      <h3>Front-end</h3>
      <ul>
        <li>Forgot Password?</li>
        <li>Configurable domain for login service urls.</li>
        <li>Review all forms + get them up to date with the new :validated function.</li>
        <li>Move the form based custom directives to EZFORM</li>
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

  >* {
    display: block;
    margin: 0 auto;
    max-width: 1500px;
  }
}

nav {
  margin: 0 0 1.0rem 0;

  a {
    margin: 0 0.25rem;
    text-decoration: none;
    border: solid 1px black;
    padding: 0.5rem;

  }

  a.router-link-active {
    background: #CCC;
  }

  a:hover {
    background: black;
    color: white;
  }

}

a {
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
