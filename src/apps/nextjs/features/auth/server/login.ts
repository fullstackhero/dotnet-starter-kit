// components/auth/loginAction.ts

"use server";

import { z } from "zod";

// Define the Zod schema for validation
const loginSchema = z.object({
  email: z.string().email(),
  password: z.string().min(6),
});

export async function loginAction(formData: {
  email: string;
  password: string;
}) {
  // Validate the input data
  const result = loginSchema.safeParse(formData);
  if (!result.success) {
    return { success: false, message: result.error.errors[0].message };
  }

  const { email, password } = result.data;

  try {
    const response = await fetch("http://localhost:5000/api/token", {
      method: "POST",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
        tenant: "root", // Include any other necessary headers
      },
      body: JSON.stringify({ email, password }),
    });

    // Check if the response is ok (status in the range 200-299)
    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(errorData.message || "Failed to login");
    }

    // Parse the response JSON
    const data = await response.json();

    return {
      success: true,
      token: data.token,
      refreshToken: data.refreshToken,
      refreshTokenExpiryTime: data.refreshTokenExpiryTime,
    };
  } catch (error) {
    // Improved error handling
    let errorMessage = "An unexpected error occurred"; // Default error message

    if (error instanceof Error) {
      errorMessage = error.message; // Get the message if it's an instance of Error
    } else if (typeof error === "string") {
      errorMessage = error; // If the error is a string, use it directly
    }

    console.error("Login error:", error);
    return {
      success: false,
      message: errorMessage,
    };
  }
}
