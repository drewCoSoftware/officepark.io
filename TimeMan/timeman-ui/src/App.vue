<template>
  <SiteHeader v-bind:loginState="loginState" />
  <router-view v-bind:loginState="loginState" />
  <!-- 
  <HelloWorld msg="Welcome to SPA hell!" count="0" />
  <button v-on:click="toggleLogin()">Toggle Login</button> -->
</template>

<script>
 import SiteHeader from "./components/SiteHeader.vue";

export default {
  name: "App",
  components: {
    SiteHeader,
  },
  data() {
    return {
      loginState: {
        isLoggedIn: true,
        loginToken: null,        // Special token to help with session tracking.
        userID: 'drew'
      },
    };
  },
  methods: {
    toggleLogin: function () {
      this.loginState.isLoggedIn = !this.loginState.isLoggedIn;
      if (this.loginState.isLoggedIn){
        this.$router.push('/sessions');
      }
    },
    // Log out any connected user, and redirect them to the homepage.
    logout: function () {
      if (this.loginState) {
        this.$router.push("/");
      }
    },
  },
  provide() {
    return {
      toggleLogin: this.toggleLogin,
      logout: this.logout,
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
