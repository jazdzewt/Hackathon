import 'package:flutter/material.dart';
<<<<<<< HEAD
import 'dashboard_page.dart';
import 'package:provider/provider.dart'; // <-- 1. Import
import 'providers/challenge_provider.dart'; 
=======
import 'pages/landing.dart';
import 'theme/colors.dart';
import 'package:go_router/go_router.dart';
import 'pages/register.dart';
>>>>>>> 4c7c15feebf2766d48462cd01394eaa1726eaeff

void main() {
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    final GoRouter _router = GoRouter(
      initialLocation: '/',
      routes: [
        GoRoute(
          path: '/',
          builder: (context, state) => const HomeScreen(),
        ),
        GoRoute(
          path: '/register',
          builder: (context, state) => const RegisterScreen(),
        ),
      ],
    );
    return MaterialApp.router(
      debugShowCheckedModeBanner: false,
      title: 'Goldman Sachs Hackathon',
      theme: ThemeData(
        appBarTheme: const AppBarTheme(
          backgroundColor: AppColors.primary,
          foregroundColor: AppColors.background,
        ),
        textTheme: const TextTheme(
          headlineLarge: TextStyle(
            fontSize: 32,
            fontWeight: FontWeight.bold,
            color: AppColors.text,
          ),
        ),
      ),
      routerConfig: _router,
    );
  }
}
