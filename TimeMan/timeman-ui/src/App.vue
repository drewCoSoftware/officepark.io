
<template>
    <div id="nav">
      <router-link to="/">Home</router-link>
      <router-link to="/about">About</router-link>
    </div>
    <router-view />
    <!-- <SiteHeader v-bind:loginState="loginState" />
  <HelloWorld msg="Welcome to SPA hell!" count="0" />
  <button v-on:click="toggleLogin()">Toggle Login</button> -->
</template>

<script>
// import { RouterLink, RouterView } from "vue-router";


// import HelloWorld from "./components/HelloWorld.vue";
// import SiteHeader from "./components/SiteHeader.vue";
// import router from "main.js"

export default {
  name: "App",
  // components: {
  //   HelloWorld,
  //   SiteHeader,
  // },
  data() {
    return {
      loginState: {
        isLoggedIn: true,
      },
    };
  },
  methods: {
    toggleLogin: function () {
      this.loginState.isLoggedIn = !this.loginState.isLoggedIn;
      //      alert('login changed! (' + this.isLoggedIn + ')');
    },
    // Log out any connected user, and redirect them to the homepage.
    logout: function() {
      if (this.loginState){
        this.$router.push('/');
      }
    }
  },
  provide() {
    return {
      toggleLogin: this.toggleLogin,
      logout: this.logout
    };
  },

  // setup (){
  //   this.isLoggedIn = false;
  // }
  // data() {
  //   return new {
  //     isLoggedIn: false
  //   }
  // }
};
</script>

<style>
#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
  margin-top: 60px;
}
</style>
